using System.Net;
using System.Net.Sockets;
using ChatClient.ServerClients;
using ChatClient.SocketClients;
using ChatClient.Tests.Mocks;
using ChatClient.Tests.Utilities;
using ChatClient.Utilities;

namespace ChatClient.Tests.Initializers;

public static class WrappedClientInitializers
{
    public static WrappedIpkClient GetTcpWrappedClient(MessageQueueManager messageQueueManager,
        ErrorQueueManager errorQueueManager)
    {
        var tcpClientMock = TcpClientMock.GetMock();
        var tcpWrappedClient = new WrappedIpkClient(
            new IpkTcpClient(tcpClientMock.Object, TcpClientMock.GetNetworkStreamMock(messageQueueManager).Object),
            new ErrorWriter(ErrorTextWriter.GetMock(errorQueueManager).Object));
        return tcpWrappedClient;
    }

    public static WrappedIpkClient GetUdpWrappedClient(MessageQueueManager queueManager,
        ErrorQueueManager errorQueueManager, byte retrials, ushort timeout)
    {
        var udpWrappedClient = new WrappedIpkClient(
            new IpkUdpClient(UdpClientMock.GetMock(queueManager).Object,
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4567),
                retrials, timeout),
            new ErrorWriter(ErrorTextWriter.GetMock(errorQueueManager).Object));
        return udpWrappedClient;
    }
}