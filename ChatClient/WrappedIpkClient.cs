using ChatClient.Models;
using ChatClient.Utilities;

namespace ChatClient;

public class WrappedIpkClient : IDisposable
{
    private readonly IIpkClient ipkClient;
    private readonly WorkflowGraph workflow;
    private string? displayName;

    public WrappedIpkClient(IIpkClient ipkClient)
    {
        this.ipkClient = ipkClient;
        this.workflow = new();
    }

    public async Task RunCommand(string command, CancellationToken cancellationToken = default)
    {
        var parts = command.Split(' ');
        
        string commandText = parts.Any() ? parts[0] : command;

        if (commandText == "/rename")
        {
            if(parts.Length < 2)
            {
                Console.Error.WriteLine("ERROR: Invalid number of arguments for /rename");
                return;
            }

            var newUsername = string.Join(' ', parts[1..]);
            
            if(newUsername.Length > 20)
            {
                Console.Error.WriteLine("ERROR: Display name must be 20 characters or less");
                return;
            }

            SetDisplayName(newUsername);

            return;
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

        if (!workflow.IsAllowedMessageType(message.MessageType))
        {
            Console.WriteLine("ERROR: Invalid state for this message");
            return;
        }
        
        switch (message.MessageType)
        {
            case MessageType.Auth:
                await Authenticate(message, cancellationToken);
                break;
            case MessageType.Join:
                await JoinChannel(message, cancellationToken);
                break;
            case MessageType.Msg:
                await SendMessage(message, cancellationToken);
                break;
        }
    }

    public async Task Leave()
    {
        await ipkClient.Leave();
        
        workflow.NextState(MessageType.Bye);
    }
    
    private async Task SendMessage(Message message, CancellationToken cancellationToken = default)
    {
        message.Arguments.Add(MessageArguments.DisplayName, displayName!);
        await ipkClient.SendMessage(message, cancellationToken);

        workflow.NextState(MessageType.Msg);
    }
    
    private async Task Authenticate(Message message, CancellationToken cancellationToken = default)
    { 
        SetDisplayName((string)message.Arguments[MessageArguments.DisplayName]);
        await ipkClient.Authenticate(message, cancellationToken);

        workflow.NextState(MessageType.Auth);
    }
    
    private async Task JoinChannel(Message message, CancellationToken cancellationToken = default)
    {
        message.Arguments.Add(MessageArguments.DisplayName, displayName!);
        await ipkClient.JoinChannel(message, cancellationToken);
        
        workflow.NextState(MessageType.Join);
    }
    
    public async Task<string?> Listen(CancellationToken cancellationToken = default)
    {
        var message = await ipkClient.Listen(cancellationToken);

        if (message.MessageType == MessageType.Unknown)
        {
            return null;
        }
        
        bool? replySuccess = null;
        
        if (message.MessageType == MessageType.Reply)
        {
            replySuccess = (bool) message.Arguments[MessageArguments.ReplyStatus];
        }
        
        workflow.NextState(message.MessageType, replySuccess);
        
        if(workflow.IsErrorState)
        {
            Message errorMessage = new()
            {
                MessageType = MessageType.Err,
                Arguments = new Dictionary<MessageArguments, object>()
                {
                    { MessageArguments.DisplayName, displayName },
                    { MessageArguments.MessageContent, "Unsupported type of message" }
                }
            };
            
            await ipkClient.SendError(errorMessage, cancellationToken);
            
            return null;
        }
        
        return message.ToString();
    }
    
    private void SetDisplayName(string displayName)
    {
        this.displayName = displayName;
    }

    public void Dispose()
    {
        ipkClient.Dispose();
    }
}