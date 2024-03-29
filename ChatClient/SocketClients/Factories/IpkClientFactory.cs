using ChatClient.Enums;

namespace ChatClient.SocketClients.Factories;

public class IpkClientFactory
{
    private readonly SocketType socketType;

    private readonly byte? udpConfirmationAttempts;
    private readonly ushort? udpConfirmationTimeout;

    public IpkClientFactory(SocketType socketType, byte? udpConfirmationAttempts = null, 
        ushort? udpConfirmationTimeout = null)
    {
        this.socketType = socketType;
        this.udpConfirmationAttempts = udpConfirmationAttempts;
        this.udpConfirmationTimeout = udpConfirmationTimeout;
    }

    public IIpkClient? CreateClient(string hostName, ushort port) => socketType switch
    {
        SocketType.Tcp => IpkTcpClient.Create(hostName, port),
        SocketType.Udp => IpkUdpClient.Create(hostName, port,
            udpConfirmationAttempts!.Value, udpConfirmationTimeout!.Value)
    };
}