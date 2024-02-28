using System.Net.Sockets;
using ChatClient.Models;
using ChatClient.Utilities.Tcp;

namespace ChatClient;

public class IpkTcpClient : IIpkClient
{
    private readonly TcpClient client;
    private readonly NetworkStream clientStream;
    private readonly TcpMessageBuilder messageBuilder = new();
    
    private IpkTcpClient(TcpClient client)
    {
        this.client = client;
        this.clientStream = client.GetStream();
    }

    public async Task SendMessage(Message message, CancellationToken cancellationToken = default)
    {
        var byteMessage = messageBuilder.GetByteMessage(message);
        
        await clientStream.WriteAsync(byteMessage, cancellationToken);
    }
    
    public async Task Authenticate(Message message, CancellationToken cancellationToken = default)
    {
        var byteMessage = messageBuilder.GetByteMessage(message);
        
        await clientStream.WriteAsync(byteMessage, cancellationToken);
    }
    
    public async Task JoinChannel(Message message, CancellationToken cancellationToken = default)
    {
        var byteMessage = messageBuilder.GetByteMessage(message);
        
        await clientStream.WriteAsync(byteMessage, cancellationToken);
    }

    public async Task SendError(Message message, CancellationToken cancellationToken = default)
    {
        var byteMessage = messageBuilder.GetByteMessage(message);
        
        await clientStream.WriteAsync(byteMessage, cancellationToken);
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

    public async Task<Message> Listen(CancellationToken cancellationToken = default)
    {
        Memory<byte> buffer = new byte[2000];
        var byteCount = await clientStream.ReadAsync(buffer, cancellationToken);
        return messageBuilder.DecodeMessage(buffer.ToArray()[..byteCount]);
    }

    public void Dispose()
    {
        clientStream.Dispose();
        client.Dispose();
    }

    public static IpkTcpClient? Create(string hostName, ushort port)
    {
        try
        {
            var client = new TcpClient();
            
            client.Connect(hostName, port);
            
            return new IpkTcpClient(client);
        }
        catch (SocketException)
        {
            return null;
        }            
    }
}