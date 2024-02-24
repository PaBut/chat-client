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

    public async Task SendMessage(Message message)
    {
        var byteMessage = messageBuilder.GetByteMessage(message);
        
        await clientStream.WriteAsync(byteMessage);
    }
    
    public async Task Authenticate(Message message)
    {
        var byteMessage = messageBuilder.GetByteMessage(message);
        
        await clientStream.WriteAsync(byteMessage);
    }
    
    public async Task JoinChannel(Message message)
    {
        var byteMessage = messageBuilder.GetByteMessage(message);
        
        await clientStream.WriteAsync(byteMessage);
    }

    public async Task SendError(Message message)
    {
        var byteMessage = messageBuilder.GetByteMessage(message);
        
        await clientStream.WriteAsync(byteMessage);
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