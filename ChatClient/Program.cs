using System.Net.Sockets;
using System.Threading.Channels;
using ChatClient;
using CommandLine;
using CommandLine.Text;
using SocketType = ChatClient.SocketType;

var parserResult = new Parser(with =>
{
    with.AutoHelp = true;
    with.HelpWriter = Console.Out;
}).ParseArguments<CommandLineOptions>(args);

if (args.Contains("-h"))
{
    Console.WriteLine(HelpText.AutoBuild(parserResult, h => h, e => e));
    return;
}

var commandLineOptions = parserResult
    .WithParsed(options =>
    {
        Console.WriteLine($"Socket type: {options.SocketType}");
        Console.WriteLine($"Server IP or hostname: {options.Host}");
        Console.WriteLine($"Server port: {options.Port}");
        Console.WriteLine($"UDP confirmation timeout: {options.UdpConfirmationTimeout}");
        Console.WriteLine($"UDP confirmation attempts: {options.UdpConfirmationAttempts}");
    }).WithNotParsed(errors => { Console.Error.WriteLine("Invalid command line options"); }).Value;

if (!Enum.TryParse<SocketType>(commandLineOptions.SocketType, true, out var socketType))
{
    Console.Error.WriteLine("Invalid command line options");
    return;
}

bool isCanceled = false;

IpkClientFactory ipkClientFactory = new(socketType, commandLineOptions.UdpConfirmationAttempts,
    commandLineOptions.UdpConfirmationTimeout);

var ipkClient = ipkClientFactory.CreateClient(commandLineOptions.Host, commandLineOptions.Port);

Console.WriteLine($"Connected to {commandLineOptions.Host} on port {commandLineOptions.Port}");

WrappedIpkClient wrappedIpkClient = new(ipkClient);

Console.CancelKeyPress += async (sender, args) =>
{
    Console.WriteLine("Exiting...");
    await wrappedIpkClient.Leave();
    args.Cancel = true;
    isCanceled = true;
};

Task senderTask = Task.Run(async () => await Sender(wrappedIpkClient));
Task receiverTask = Task.Run(async () => await Receiver(wrappedIpkClient));

await Task.WhenAll(senderTask, receiverTask);

async Task Sender(WrappedIpkClient wrappedClient)
{
    while (!isCanceled)
    {
        var userInput = Console.ReadLine();

        if (string.IsNullOrEmpty(userInput))
        {
            return;
        }

        await wrappedClient.RunCommand(userInput);
    }
}

async Task Receiver(WrappedIpkClient wrappedClient)
{
    while (!isCanceled)
    {
        var response = await wrappedClient.Listen();

        if(string.IsNullOrEmpty(response))
        {
            return;
        }
        
        Console.Write(response);
    }
}

Console.ReadKey();