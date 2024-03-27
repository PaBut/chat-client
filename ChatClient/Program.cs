using ChatClient;
using CommandLine;
using CommandLine.Text;
using SocketType = ChatClient.SocketType;

var parserResult = new Parser(with =>
{
    with.AutoHelp = false;
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

IpkClientFactory ipkClientFactory = new(socketType, commandLineOptions.UdpConfirmationAttempts,
    commandLineOptions.UdpConfirmationTimeout);

IIpkClient? ipkClient = null;

// for (int i = 0; i < 100 && ipkClient == null; i++)
// {
    ipkClient = ipkClientFactory.CreateClient(commandLineOptions.Host, commandLineOptions.Port);
//     Console.WriteLine($"Debug: {i}th try");
//     if (ipkClient != null)
//     {
//         Console.WriteLine("Finally");
//     }
// }


if (ipkClient == null)
{
    Console.WriteLine("ERROR: Connection refused");
    return;
}

Console.WriteLine($"Connected to {commandLineOptions.Host} on port {commandLineOptions.Port}");

CancellationTokenSource cancellationTokenSource = new();

WrappedIpkClient wrappedIpkClient = new(ipkClient, () =>
{
    cancellationTokenSource.Cancel();
    cancellationTokenSource.Dispose();
});

var token = cancellationTokenSource.Token;

Console.CancelKeyPress += async (sender, args) =>
{
    await SendByeAndDisposeElements();
    Console.WriteLine("Exiting...");
    args.Cancel = true;
};

Task senderTask = Task.Run(async () => await Sender(wrappedIpkClient, token));
Task receiverTask = Task.Run(async () => await Receiver(wrappedIpkClient, token));

try
{
    await Task.WhenAny(senderTask, receiverTask);
    await SendByeAndDisposeElements();
}
catch (Exception) { }

async Task Sender(WrappedIpkClient wrappedClient, CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var userInput = await Console.In.ReadLineAsync(cancellationToken);

        if (userInput == string.Empty)
        {
            continue;
        }

        if (userInput == null)
        {
            return;
        }

        await wrappedClient.RunCommand(userInput, cancellationToken);
    }
}

async Task Receiver(WrappedIpkClient wrappedClient, CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var result = await wrappedClient.Listen(cancellationToken);
        
        if(result != null && result.Value.Message != null)
        {
            if (result.Value.isError)
            {
                Console.Error.WriteLine(result.Value.Message);
            }
            else
            {
                Console.WriteLine(result.Value.Message);
            }
        }
    }
}

async Task SendByeAndDisposeElements()
{
    await wrappedIpkClient.Leave();
    if (!token.IsCancellationRequested)
    {
        cancellationTokenSource.Cancel();
    }
    cancellationTokenSource.Dispose();
    wrappedIpkClient.Dispose();
}
