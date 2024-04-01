using System.Net;
using ChatClient.Enums;
using ChatClient.Exceptions;
using ChatClient.Models;
using ChatClient.SocketClients.Proxies.Udp;
using ChatClient.SocketClients.Utilities.Udp;
using UdpClient = System.Net.Sockets.UdpClient;

namespace ChatClient.SocketClients;

public class IpkUdpClient : IIpkClient
{
    private readonly IUdpClientProxy client;
    private readonly UdpMessageCoder messageCoder = new();
    private readonly ushort timeout;
    private readonly byte retrials;

    private bool portUpdated = false;
    private IPEndPoint remoteEndPoint;
    private ushort currentMessageId = 0;
    private List<ushort> seenMessages = new();
    private List<ushort> confirmedMessages = new();

    public IpkUdpClient(IUdpClientProxy client, IPEndPoint endpoint, byte retrials, ushort timeout)
    {
        this.client = client;
        this.retrials = retrials;
        this.timeout = timeout;
        this.remoteEndPoint = endpoint;
    }

    public async Task SendMessage(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = currentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageCoder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage, cancellationToken);
    }

    public async Task Authenticate(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = currentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageCoder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage, cancellationToken);
    }

    public async Task JoinChannel(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = currentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageCoder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage, cancellationToken);
    }

    public async Task SendError(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = currentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageCoder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage, cancellationToken);
    }

    public async Task Leave()
    {
        var messageId = currentMessageId++;

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

            if (!confirmedMessages.Contains(messageId))
            {
                confirmedMessages.Add(messageId);
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
            if (seenMessages.Contains(messageId))
            {
                return new ResponseResult(message, ResponseProcessingResult.AlreadyProcessed);
            }
            seenMessages.Add(messageId);
        }

        return new ResponseResult(message);
    }

    private async Task SendWithRetrial(ushort messageId, byte[] message, CancellationToken cancellationToken = default)
    {

        for (int i = 0; i < retrials + 1 && !confirmedMessages.Contains(messageId); i++)
        {
            await client.SendAsync(message, message.Length, remoteEndPoint);

            await Task.WhenAny(Task.Delay(timeout, cancellationToken), Task.Run(() =>
            {
                while (!confirmedMessages.Contains(messageId)) { }
            }));
        }

        if (!confirmedMessages.Contains(messageId))
        {
            throw new NotReceivedConfirmException();
        }
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

            return new IpkUdpClient(new UdpClientProxy(client), endpoint, retrials, timeout);
        }
        catch (Exception)
        {
            return null;
        }
    }
}