using CommandLine;

namespace ChatClient;

public class CommandLineOptions
{
    [Option('t', "type", Required = true, HelpText = "Socket type (tcp, udp)")]
    public string SocketType{ get; set; }
    
    [Option('s', "", Required = true, HelpText = "Server IP or hostname")]
    public string Host { get; set; }
    
    [Option('p', "port",  Required = false, HelpText = "Server port", Default = (short)4)]
    public short Port { get; set; }
    
    [Option('u', "udp-timeout", Required = false, HelpText = "UDP confirmation timeout", Default = (short)250)]
    public short UdpConfirmationTimeout { get; set; }
    
    [Option('r', "udp-retrials", Required = false, HelpText = "UDP confirmation attempts", Default = (byte)3)]
    public byte UdpConfirmationAttempts { get; set; }
}