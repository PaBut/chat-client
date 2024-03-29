using CommandLine;

namespace ChatClient.Models;

public class CommandLineOptions
{
    [Option('t', Required = true, HelpText = "Socket type (tcp, udp)")]
    public string SocketType { get; set; } = null!;

    [Option('s', Required = true, HelpText = "Server IP or hostname")]
    public string Host { get; set; } = null!;

    [Option('p', Required = false, HelpText = "Server port", Default = (ushort)4567)]
    public ushort Port { get; set; }

    [Option('u', Required = false, HelpText = "UDP confirmation timeout", Default = (ushort)250)]
    public ushort UdpConfirmationTimeout { get; set; }

    [Option('r', Required = false, HelpText = "UDP confirmation attempts", Default = (byte)3)]
    public byte UdpConfirmationAttempts { get; set; }
}