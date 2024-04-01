using System.Net.Sockets;
using ChatClient.SocketClients;
using ChatClient.Tests.Utilities;
using Moq;

namespace ChatClient.Tests.Mocks;

public static class TcpClientMock
{
    private static Queue<string> incomingMessageQueue = new();
    private static Queue<string> outcomingMessageQueue = new();
    private static object lockObject = new();
    
    public static Mock<TcpClient> GetMock()
    {
        var mock = new Mock<TcpClient>();
        
        return mock;
    }
    
    public static Mock<ITcpNetworkWriterProxy> GetNetworkStreamMock(MessageQueueManager queueManager)
    {
        var streamMock = new Mock<ITcpNetworkWriterProxy>();

        streamMock.Setup(x => x.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<ReadOnlyMemory<byte>, CancellationToken>((bytes, token) =>
            {
                queueManager.SendMessageToServer(bytes.ToArray());
            });
        
        streamMock.Setup(x => x.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory<byte> buffer, CancellationToken cancellationToken) =>
            {
                var message = queueManager.GetReceivedMessage();
                message.CopyTo(buffer);
                return message.Length;
            });
        
        return streamMock;
    }
}