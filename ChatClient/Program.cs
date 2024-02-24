using System.Net.Sockets;
using System.Threading.Channels;
using ChatClient;
using CommandLine;
using CommandLine.Text;

var parserResult = new Parser(with =>
{
    with.AutoHelp = true;
    with.HelpWriter = new StreamWriter(Console.OpenStandardOutput());
}).ParseArguments<CommandLineOptions>(args);
var commandLineOptions = parserResult
    .WithParsed(options =>
    {
        Console.WriteLine($"Socket type: {options.SocketType}");
        Console.WriteLine($"Server IP or hostname: {options.Host}");
        Console.WriteLine($"Server port: {options.Port}");
        Console.WriteLine($"UDP confirmation timeout: {options.UdpConfirmationTimeout}");
        Console.WriteLine($"UDP confirmation attempts: {options.UdpConfirmationAttempts}");
    }).WithNotParsed(errors =>
    {
        if(errors.Any(error => error.Tag == ErrorType.HelpRequestedError))
        {
            Console.WriteLine(HelpText.AutoBuild(parserResult, h =>
            {
                //configure HelpText
                h.AdditionalNewLineAfterOption = false; //remove newline between options
                h.Heading = "Myapp 2.0.0-beta"; //change header
                h.Copyright = "Copyright (c) 2019 Global.com"; //change copyright text
                // more options ...
                return h;
            }, e => e));
        }
        Console.WriteLine("Invalid command line options");
    });

var sender = new Thread(Sender);

IIpkClient ipkClient = default;

static void Sender()
{
    while (true)
    {
        var userInput = Console.ReadLine();

        if (string.IsNullOrEmpty(userInput))
        {
            return;
        }

        if (userInput[0] != '/')
        {
            ipkClient.SendMessage(userInput);
        }
        
    }
}

static 