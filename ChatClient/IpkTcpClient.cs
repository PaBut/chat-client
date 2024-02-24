using System.Net.Sockets;
using ChatClient.Models;
using ChatClient.Utilities.Tcp;

namespace ChatClient;

public class IpkTcpClient : IIpkClient
{
    private readonly TcpClient client;
    private readonly NetworkStream clientStream;
    private readonly TcpMessageBuilder messageBuilder = new();
    
    public IpkTcpClient(string hostName, ushort port)
    {
        client = new TcpClient(hostName, port);
        clientStream = client.GetStream();
    }

    public async Task SendMessage(string message, string displayName)
    {
        var messageString = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Msg,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, displayName },
                { MessageArguments.MessageContent, message },
            }
        });
        
        await clientStream.WriteAsync(messageString);
    }
    
    public async Task Authenticate(string userName, string secret, string displayName)
    {
        var messageString = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Auth,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.UserName, userName },
                { MessageArguments.DisplayName, displayName },
                { MessageArguments.Secret, secret },
            }
        });
        
        await clientStream.WriteAsync(messageString);
    }
    
    public async Task JoinChannel(string channelId, string displayName)
    {
        var messageString = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Join,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.ChannelId, channelId },
                { MessageArguments.DisplayName, displayName },
            }
        });
        
        await clientStream.WriteAsync(messageString);
    }

    public async Task Leave()
    {
        var messageString = messageBuilder.GetByteMessage(new Message()
        {
            MessageType = MessageType.Bye,
            Arguments = new Dictionary<MessageArguments, object>()
        });
        
        await clientStream.WriteAsync(messageString);
    }

    public async Task<Message> Listen()
    {
        Memory<byte> buffer = new byte[2000];
        var byteCount = await clientStream.ReadAsync(buffer);
        return messageBuilder.DecodeMessage(buffer.ToArray()[..byteCount]);
    }

    public void Dispose()
    {
        client.Dispose();
        clientStream.Dispose();
    }
}