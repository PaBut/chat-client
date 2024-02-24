using ChatClient.Models;
using ChatClient.Utilities;

namespace ChatClient;

public class WrappedIpkClient : IDisposable
{
    private readonly IIpkClient ipkClient;
    private readonly WorkflowGraph workflow = new();
    private string displayName;

    public WrappedIpkClient(IIpkClient ipkClient)
    {
        this.ipkClient = ipkClient;
    }

    public async Task RunCommand(string command)
    {
        var parts = command.Split(' ');
        
        string commandText = parts.Any() ? parts[0] : command;

        if (commandText == "/rename")
        {
            if(parts.Length != 2)
            {
                Console.Error.WriteLine("ERROR: Invalid number of arguments for /rename");
                return;
            }
        }
        else if (commandText == "/help")
        {
            Console.WriteLine("Help message");
            return;
        }
        
        var message = Message.FromCommandLine(command, out var errorResponse);

        if (message.MessageType == MessageType.Unknown)
        {
            Console.Error.WriteLine($"ERROR: {errorResponse}");
        }
        
        
    }

    public async Task Leave()
    {
        if (workflow.IsAllowedMessageType(MessageType.Bye))
        {
            await ipkClient.Leave();
        }
        
        workflow.NextState(MessageType.Bye);
    }
    
    private async Task SendMessage(string message)
    {
        if (workflow.IsAllowedMessageType(MessageType.Msg))
        {
            await ipkClient.SendMessage(message, displayName);
        }

        workflow.NextState(MessageType.Msg);
    }
    
    private async Task Authenticate(string userName, string secret, string displayName)
    {
        if (workflow.IsAllowedMessageType(MessageType.Auth))
        {
            SetDisplayName(displayName);
            await ipkClient.Authenticate(userName, secret, displayName);
        }

        workflow.NextState(MessageType.Auth);
    }
    
    private async Task JoinChannel(string channelId)
    {
        if (workflow.IsAllowedMessageType(MessageType.Join))
        {
            await ipkClient.JoinChannel(channelId, displayName);
        }

        workflow.NextState(MessageType.Join);
    }
    
    public async Task<string?> Listen()
    {
        var message = await ipkClient.Listen();
        
        bool? replySuccess = null;
        
        if (message.MessageType == MessageType.Reply)
        {
            replySuccess = (bool) message.Arguments[MessageArguments.ReplyStatus];
        }
        
        workflow.NextState(message.MessageType, replySuccess);
        
        return message.ToString();
    }
    
    public void SetDisplayName(string displayName)
    {
        this.displayName = displayName;
    }

    public void Dispose()
    {
        ipkClient.Dispose();
    }
}