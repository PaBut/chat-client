using System.Net.Sockets;
using ChatClient.Models;
using ChatClient.Utilities.Udp;

namespace ChatClient;

public class IpkUdpClient : IIpkClient
{
    private readonly UdpClient client;
    private readonly UdpMessageBuilder messageBuilder = new();
    private readonly ushort timeout;
    private readonly byte retrials;

    private ushort CurrentMessageId = 0;
    private List<ushort> SeenMessages = new();
    
    public IpkUdpClient(string hostName, ushort port, byte retrials, ushort timeout)
    {
        client = new UdpClient(hostName, port);
        this.retrials = retrials;
        this.timeout = timeout;
    }

    public async Task SendMessage(Message message)
    {
        var messageId = CurrentMessageId++;
        
        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageBuilder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage);
    }

    public async Task Authenticate(Message message)
    {
        var messageId = CurrentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageBuilder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage);
    }

    public async Task JoinChannel(Message message)
    {
        var messageId = CurrentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageBuilder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage.ToArray());
    }

    public async Task SendError(Message message)
    {
        var messageId = CurrentMessageId++;

        message.Arguments.Add(MessageArguments.MessageId, messageId);

        var byteMessage = messageBuilder.GetByteMessage(message);

        await SendWithRetrial(messageId, byteMessage.ToArray());
    }

    public async Task Leave()
    {
        var messageId = CurrentMessageId++;

        var byteMessage = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Bye,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.MessageId, messageId }
            }
        });

        await SendWithRetrial(messageId, byteMessage);
    }

    public async Task<Message> Listen()
    {
        var response = await client.ReceiveAsync();

        var message = messageBuilder.DecodeMessage(response.Buffer);

        if (message.MessageType == MessageType.Confirm)
        {
            var messageId = (ushort)message.Arguments[MessageArguments.ReferenceMessageId];
            if (!SeenMessages.Contains(messageId))
            {
                SeenMessages.Add(messageId);
            }
        }
        else
        {
            var messageId = (ushort)message.Arguments[MessageArguments.MessageId];
            await SendConfirmation(messageId);
        }

        return message;
    }

    private async Task SendWithRetrial(ushort messageId, byte[] message)
    {
        for (int i = 0; i < retrials + 1; i++)
        {
            await client.SendAsync(message.ToArray(), message.Length);

            var task = client.ReceiveAsync();

            if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromMilliseconds(timeout))) == task)
            {
                var response = await task;
                if (response.Buffer.Length > 2 && response.Buffer[0] == 0 &&
                    BitConverter.ToInt16(response.Buffer[1..3], 0) == messageId)
                {
                    if (!SeenMessages.Contains(messageId))
                    {
                        SeenMessages.Add(messageId);
                    }

                    return;
                }
            }
        }
    }

    private async Task SendConfirmation(ushort messageId)
    {
        var byteMessage = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Confirm,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.ReferenceMessageId, messageId },
            }
        });

        await client.SendAsync(byteMessage, byteMessage.Length);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}