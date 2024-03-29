using System.Net.Sockets;
using ChatClient;
using ChatClient.Exceptions;
using ChatClient.Utilities;
using CommandLine;
using CommandLine.Text;
using SocketType = ChatClient.SocketType;

var errorWriter = new ErrorWriter(Console.Error);

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
    }).WithNotParsed(errors => { errorWriter.WriteError("Invalid command line options"); }).Value;

if (!Enum.TryParse<SocketType>(commandLineOptions.SocketType, true, out var socketType))
{
    errorWriter.WriteError("Invalid command line options");
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
    errorWriter.WriteError("Connection refused");
    return;
}

Console.WriteLine($"Connected to {commandLineOptions.Host} on port {commandLineOptions.Port}");

CancellationTokenSource cancellationTokenSource = new();

WrappedIpkClient wrappedIpkClient = new(ipkClient, () =>
{
    cancellationTokenSource.Cancel();
    cancellationTokenSource.Dispose();
}, errorWriter);

var token = cancellationTokenSource.Token;

// Console.CancelKeyPress += async (sender, args) =>
// {
//     await SendByeAndDisposeElements();
//     Console.WriteLine("Exiting...");
//     args.Cancel = true;
// };

Task senderTask = Task.Run(async () => await Sender(wrappedIpkClient, token));
Task receiverTask = Task.Run(async () => await Receiver(wrappedIpkClient, token));

try
{
    await Task.WhenAny(senderTask, receiverTask);
}
catch (TaskCanceledException) { }

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
            await SendByeAndDisposeElements();
            return;
        }

        try
        {
            await wrappedClient.RunCommand(userInput, cancellationToken);
        }
        catch (SocketException)
        {
            errorWriter.WriteError("Socket exception");
            return;
        }

    }
}

async Task Receiver(WrappedIpkClient wrappedClient, CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            var result = await wrappedClient.Listen(cancellationToken);

            if (result != null && result.Value.Message != null)
            {
                if (result.Value.isError)
                {
                    errorWriter.WriteError(result.Value.Message);
                }
                else
                {
                    Console.WriteLine(result.Value.Message);
                }
            }
        }
        catch (SocketException)
        {
            errorWriter.WriteError("Socket exception");
            return;
        }
    }
}

async Task SendByeAndDisposeElements()
{
    if (!token.IsCancellationRequested)
    {
        cancellationTokenSource.Cancel();
    }
    await wrappedIpkClient.Leave();
    cancellationTokenSource.Dispose();
    wrappedIpkClient.Dispose();
}
