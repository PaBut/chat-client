using System.Net.Sockets;
using ChatClient.Models;
using ChatClient.ServerClients;
using ChatClient.SocketClients;
using ChatClient.SocketClients.Factories;
using ChatClient.Utilities;
using CommandLine;
using CommandLine.Text;
using SocketType = ChatClient.Enums.SocketType;

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

var commandLineOptions = parserResult.Value;

if (parserResult.Errors.Any() || !Enum.TryParse<SocketType>
        (commandLineOptions.SocketType, true, out var socketType))
{
    errorWriter.WriteError("Invalid command line options");
    Environment.ExitCode = 1;
    return;
}

IpkClientFactory ipkClientFactory = new(socketType, commandLineOptions.UdpConfirmationAttempts,
    commandLineOptions.UdpConfirmationTimeout);

IIpkClient? ipkClient = ipkClientFactory.CreateClient(commandLineOptions.Host, commandLineOptions.Port);

if (ipkClient == null)
{
    errorWriter.WriteError("Connection can not be instantiated");
    Environment.ExitCode = 1;
    return;
}

CancellationTokenSource cancellationTokenSource = new();

WrappedIpkClient wrappedIpkClient = new(ipkClient, errorWriter);

var token = cancellationTokenSource.Token;
var waitForByeSent = new ManualResetEvent(false);
var locker = new object();
var isEndStateProcessing = false;

Console.CancelKeyPress += (sender, args) =>
{
    lock (locker)
    {
        if (!isEndStateProcessing)
        {
            isEndStateProcessing = true;
            SendByeAndDisposeElements().GetAwaiter().GetResult();
        }
    }
    args.Cancel = true;
};

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
            lock (locker)
            {
                if (!isEndStateProcessing)
                {
                    isEndStateProcessing = true;
                    SendByeAndDisposeElements().GetAwaiter().GetResult();
                }
            }
            return;
        }

        try
        {
            await wrappedClient.RunCommand(userInput, cancellationToken);
        }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            errorWriter.WriteError("Socket exception");
            DisposeElements();
            Environment.ExitCode = 1;
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

            if (result.HasValue && result.Value.ByeReceived)
            {
                DisposeElements();
                return;
            }
            
            if (result != null && result.Value.Message != null)
            {
                if (result.Value.ToStderr)
                {
                    errorWriter.Write(result.Value.Message);
                    if (result.Value.IsServerError)
                    {
                        await SendByeAndDisposeElements();
                        return;
                    }
                }
                else
                {
                    Console.WriteLine(result.Value.Message);
                }
            }
        }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            errorWriter.WriteError("Socket exception");
            DisposeElements();
            Environment.ExitCode = 1;
            return;
        }
    }
}

async Task SendByeAndDisposeElements()
{
    await wrappedIpkClient.Leave();
    DisposeElements();
    waitForByeSent.Set();
}

void DisposeElements()
{
    if (!token.IsCancellationRequested)
    {
        cancellationTokenSource.Cancel();
    }
    cancellationTokenSource.Dispose();
    wrappedIpkClient.Dispose();
}