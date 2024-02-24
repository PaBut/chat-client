using System.Net.Sockets;
using ChatClient.Models;
using ChatClient.Utilities.Udp;

namespace ChatClient;

public class IpkUdpClient : IIpkClient
{
    private readonly UdpClient client;
    private readonly UdpMessageBuilder messageBuilder = new();
    private readonly int timeout;
    private readonly int retrials;

    private ushort CurrentMessageId = 0;
    private List<ushort> SeenMessages = new();
    
    public IpkUdpClient(ushort port, string hostName, short retrials, byte timeout)
    {
        client = new UdpClient(hostName, port);
        this.retrials = retrials;
        this.timeout = timeout;
    }

    public async Task SendMessage(string message, string displayName)
    {
        var currentMessageId = CurrentMessageId++;

        var byteMessage = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Msg,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, displayName },
                { MessageArguments.MessageContent, message },
            }
        });

        await SendWithRetrial(currentMessageId, byteMessage);
    }

    public async Task Authenticate(string userName, string secret, string displayName)
    {
        var messageId = CurrentMessageId++;

        var byteMessage = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Auth,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.UserName, userName },
                { MessageArguments.DisplayName, displayName },
                { MessageArguments.Secret, secret },
            }
        });

        await SendWithRetrial(messageId, byteMessage);
    }

    public async Task JoinChannel(string channelId, string displayName)
    {
        var messageId = CurrentMessageId++;

        var byteMessage = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Join,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.ChannelId, channelId },
                { MessageArguments.DisplayName, displayName },
            }
        });

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