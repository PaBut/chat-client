using System.Net;
using System.Net.Sockets;

namespace ChatClient.SocketClients.Proxies.Udp;

public class UdpClientProxy : IUdpClientProxy
{
    private readonly UdpClient client;

    public UdpClientProxy(UdpClient client)
    {
        this.client = client;
    }

    public ValueTask<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return client.ReceiveAsync(cancellationToken);
    }

    public Task<int> SendAsync(byte[] bytes, int length, IPEndPoint? endPoint)
    {
        return client.SendAsync(bytes, length, endPoint);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}