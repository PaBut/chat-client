using System.Net;
using System.Net.Sockets;
using ChatClient.SocketClients.Proxies.Udp;
using ChatClient.Tests.Utilities;
using Moq;

namespace ChatClient.Tests.Mocks;

public static class UdpClientMock
{
    public static Mock<IUdpClientProxy> GetMock(MessageQueueManager queueManager)
    {
        var mock = new Mock<IUdpClientProxy>();

        mock.Setup(x => x.SendAsync(It.IsAny<byte[]>(),
                It.IsAny<int>(), It.IsAny<IPEndPoint?>()))
            .ReturnsAsync((byte[] datagram, int count, IPEndPoint? endPoint) =>
            {
                queueManager.SendMessageToServer(datagram);
                return count;
            });

        mock.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken cancellationToken) =>
                new UdpReceiveResult(queueManager.GetReceivedMessage(),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4567)));

        return mock;
    }
}