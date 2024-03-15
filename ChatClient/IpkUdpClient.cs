using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ChatClient.Models;
using ChatClient.Utilities.Udp;
using UdpClient = System.Net.Sockets.UdpClient;

namespace ChatClient;

public class IpkUdpClient : IIpkClient
{
    // private class ConfirmTimer
    // {
    //     public Timer Timer { get; }
    //     public int Retrials { get; set; }
    //     public ManualResetEvent ResetEvent { get; }
    //
    //     public ConfirmTimer(Timer timer, ManualResetEvent resetEvent, int retrials)
    //     {
    //         Timer = timer;
    //         ResetEvent = resetEvent;
    //         Retrials = retrials;
    //     }
    // }
    
    private readonly UdpClient client;
    private readonly UdpMessageCoder messageCoder = new();
    //private readonly IDictionary<ushort, ConfirmTimer> timers;
    private readonly ushort timeout;
    private readonly byte retrials;

    private bool portUpdated = false;
    private IPEndPoint remoteEndPoint;
    private ushort CurrentMessageId = 0;
    private List<ushort> SeenMessages = new();
    private List<ushort> ConfirmedMessages = new();

    private IpkUdpClient(UdpClient client, IPEndPoint endpoint, byte retrials, ushort timeout)
    {
        this.client = client;
        this.retrials = retrials;
        this.timeout = timeout;
        this.remoteEndPoint = endpoint;
        //timers = new ConcurrentDictionary<ushort, ConfirmTimer>();
    }

    public async Task SendMessage(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = CurrentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageCoder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage, cancellationToken);
    }

    public async Task Authenticate(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = CurrentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageCoder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage, cancellationToken);
    }

    public async Task JoinChannel(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = CurrentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageCoder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage, cancellationToken);
    }

    public async Task SendError(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = CurrentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageCoder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage, cancellationToken);
    }

    public async Task Leave()
    {
        var messageId = CurrentMessageId++;

        var byteMessage = messageCoder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Bye,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.MessageId, messageId }
            }
        });

        await SendWithRetrial(messageId, byteMessage);
    }

    public async Task<ResponseResult> Listen(CancellationToken cancellationToken = default)
    {
        var response = await client.ReceiveAsync(cancellationToken);

        var message = messageCoder.DecodeMessage(response.Buffer);

        if (message.MessageType == MessageType.Unknown)
        {
            return new ResponseResult(message, ResponseProcessingResult.ParsingError);
        }

        if (message.MessageType == MessageType.Confirm)
        {
            var messageId = (ushort)message.Arguments[MessageArguments.ReferenceMessageId];
            // if (timers.ContainsKey(messageId))
            // {
                //var timerEntry = timers[messageId];
                // timerEntry.ResetEvent.Set();
                // await timerEntry.Timer.DisposeAsync();
                //timers.Remove(messageId);
            // }

            if (!ConfirmedMessages.Contains(messageId))
            {
                ConfirmedMessages.Add(messageId);
            }
        }
        else
        {
            if (!portUpdated)
            {
                remoteEndPoint.Port = response.RemoteEndPoint.Port;
                portUpdated = true;
            }
            var messageId = (ushort)message.Arguments[MessageArguments.MessageId];
            await SendConfirmation(messageId, cancellationToken);
            if (SeenMessages.Contains(messageId))
            {
                return new ResponseResult(message, ResponseProcessingResult.AlreadyProcessed);
            }
            SeenMessages.Add(messageId);
        }

        return new ResponseResult(message);
    }

    private async Task SendWithRetrial(ushort messageId, byte[] message, CancellationToken cancellationToken = default)
    {

        for (int i = 0; i < retrials + 1 && !ConfirmedMessages.Contains(messageId); i++)
        {
            await client.SendAsync(message, message.Length, remoteEndPoint);

            await Task.WhenAny(Task.Delay(timeout, cancellationToken), Task.Run(() =>
            {
                while (!ConfirmedMessages.Contains(messageId)) { }
            }));
        }
        
        // var resetEvent = new ManualResetEvent(false);
        //
        // var timer = new Timer(
        //     async (state) =>
        //     {
        //         if (!timers.ContainsKey(messageId))
        //         {
        //             return;
        //         }
        //         if (timers[messageId].Retrials-- <= 0)
        //         {
        //             resetEvent.Set();
        //             await timers[messageId].Timer.DisposeAsync();
        //         }
        //
        //         await client.SendAsync(message, message.Length, remoteEndPoint);
        //     }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(timeout));
        //
        // timers.Add(messageId, new ConfirmTimer(timer, resetEvent, retrials));
        //
        // resetEvent.WaitOne();

        // for (int i = 0; i < retrials + 1 && !SeenMessages.Contains(messageId); i++)
        // {
        //     await client.SendAsync(message, message.Length, remoteEndPoint);
        //
        //     var task = client.ReceiveAsync(cancellationToken).AsTask();
        //
        //     if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromMilliseconds(timeout), cancellationToken)) == task)
        //     {
        //         var response = await task;
        //         
        //         if (response.Buffer.Length > 2 && response.Buffer[0] == 0 &&
        //             BitConverter.ToInt16(response.Buffer[1..3], 0) == messageId)
        //         {
        //             SeenMessages.Add(messageId);
        //             return;
        //         }
        //     }
        // }
    }

    private async Task SendConfirmation(ushort messageId, CancellationToken cancellationToken = default)
    {
        var byteMessage = messageCoder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Confirm,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.ReferenceMessageId, messageId },
            }
        });

        await client.SendAsync(byteMessage, byteMessage.Length, remoteEndPoint);
    }

    public void Dispose()
    {
        client.Dispose();
    }

    public static IpkUdpClient? Create(string hostName, ushort port, byte retrials, ushort timeout)
    {
        try
        {
            IPAddress? ipAddress;
            if (!IPAddress.TryParse(hostName, out ipAddress))
            {
                ipAddress = Dns.GetHostAddresses(hostName)[0];
            }

            var endpoint = new IPEndPoint(ipAddress, port);

            var client = new UdpClient();
            client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            //client.Client.Listen();

            return new IpkUdpClient(client, endpoint, retrials, timeout);
        }
        catch (Exception)
        {
            return null;
        }
    }
}