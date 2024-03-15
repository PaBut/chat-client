using System.Net.Sockets;
using ChatClient.Models;
using ChatClient.Utilities.Tcp;

namespace ChatClient;

public class IpkTcpClient : IIpkClient
{
    private readonly TcpClient client;
    private readonly NetworkStream clientStream;
    private readonly TcpMessageBuilder messageBuilder;
    private readonly TcpMessageQueue messageQueue;
    
    private IpkTcpClient(TcpClient client)
    {
        this.client = client;
        this.clientStream = client.GetStream();
        messageBuilder = new TcpMessageBuilder();
        messageQueue = new TcpMessageQueue(messageBuilder);
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

    public async Task<ResponseResult> Listen(CancellationToken cancellationToken = default)
    {
        var message = messageQueue.Dequeue();
        if (message == null)
        {
            Memory<byte> buffer = new byte[2000];
            var byteCount = await clientStream.ReadAsync(buffer, cancellationToken);
            messageQueue.Enqueue(buffer.ToArray()[..byteCount]);
            message = messageQueue.Dequeue();
        }
        
        var processingResult = ResponseProcessingResult.Ok;
        if (message!.MessageType == MessageType.Unknown)
        {
            processingResult = ResponseProcessingResult.ParsingError;
        }
        return new ResponseResult(message, processingResult);
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