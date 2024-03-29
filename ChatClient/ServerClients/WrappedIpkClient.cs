using ChatClient.Enums;
using ChatClient.Exceptions;
using ChatClient.Models;
using ChatClient.Models.Validation;
using ChatClient.ServerClients.Utilities;
using ChatClient.SocketClients;
using ChatClient.Utilities;

namespace ChatClient.ServerClients;

public class WrappedIpkClient : IDisposable
{
    private readonly IIpkClient ipkClient;
    private readonly WorkflowGraph workflow;
    private readonly MessageValidator messageValidator = new();
    private readonly ErrorWriter errorWriter;
    
    private string? displayName;
    private bool awaitReply = false;
    
    private const string helpText = """
                                    Available commands:
                                    * /help - Display help message
                                    * /auth <username> <secret> <displayName> - Authenticate with the server
                                    * /join <channelId> - Join a channel
                                    * <message> - Send a message
                                    """;

    public WrappedIpkClient(IIpkClient ipkClient, ErrorWriter errorWriter)
    {
        this.ipkClient = ipkClient;
        this.errorWriter = errorWriter;
        this.workflow = new();
    }

    public async Task RunCommand(string command, CancellationToken cancellationToken = default)
    {
        var parts = command.Split(' ');

        string commandText = parts.Any() ? parts[0] : command;

        if (commandText == "/rename")
        {
            if (parts.Length != 2)
            {
                errorWriter.WriteError("Invalid number of arguments for /rename");
                return;
            }

            var newUsername = parts[1];

            if (newUsername.Length > 20)
            {
                errorWriter.WriteError("Display name must be 20 characters or less");
                return;
            }

            SetDisplayName(newUsername);

            return;
        }

        if (commandText == "/help")
        {
            Console.WriteLine(helpText);
            return;
        }

        var message = Message.FromCommandLine(command, out var errorResponse);

        if (message.MessageType == MessageType.Unknown)
        {
            errorWriter.WriteError(errorResponse);
            return;
        }

        if (!messageValidator.IsValid(message))
        {
            errorWriter.WriteError("Invalid message format");
            return;
        }

        if (!workflow.IsAllowedMessageType(message.MessageType))
        {
            errorWriter.WriteError("Invalid state for this message");
            return;
        }

        try
        {
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
        catch (NotReceivedConfirmException)
        {
            errorWriter.WriteError("Error: Server did not receive your message");
        }
    }

    public async Task Leave()
    {
        if (workflow.IsAuthenticated)
        {
            await ipkClient.Leave();
        }

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
        AwaitForReply();
    }

    private async Task JoinChannel(Message message, CancellationToken cancellationToken = default)
    {
        message.Arguments.Add(MessageArguments.DisplayName, displayName!);
        await ipkClient.JoinChannel(message, cancellationToken);
        workflow.NextState(MessageType.Join);
        AwaitForReply();
    }

    private async Task SendErrorMessage(string errorMessage, CancellationToken cancellationToken = default)
    {
        Message message = new()
        {
            MessageType = MessageType.Err,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, displayName ?? "" },
                { MessageArguments.MessageContent, errorMessage }
            }
        };

        await ipkClient.SendError(message, cancellationToken);
    }

    private void AwaitForReply()
    {
        awaitReply = true;
        while (awaitReply)
        {
        }
    }

    private void UnblockAwait()
    {
        awaitReply = false;
    }

    public async Task<(string? Message, bool ToStderr, bool ByeReceived)?> Listen(
        CancellationToken cancellationToken = default)
    {
        var result = await ipkClient.Listen(cancellationToken);

        if (result.ProcessingResult == ResponseProcessingResult.AlreadyProcessed)
        {
            return null;
        }

        if (result.ProcessingResult == ResponseProcessingResult.ParsingError)
        {
            await SendErrorMessage("Failed to parse request", cancellationToken);
            workflow.SetToErrorState();

            return null;
        }

        if (!messageValidator.IsValid(result.Message))
        {
            await SendErrorMessage("Message is not valid", cancellationToken);
            workflow.SetToErrorState();

            return null;
        }

        var message = result.Message;

        bool? replySuccess = null;

        if (message.MessageType == MessageType.Reply)
        {
            replySuccess = (bool)message.Arguments[MessageArguments.ReplyStatus];
        }

        workflow.NextState(message.MessageType, replySuccess);

        if (workflow.IsErrorState && message.MessageType != MessageType.Err)
        {
            await SendErrorMessage("Invalid state for this message", cancellationToken);

            return null;
        }

        if (message.MessageType == MessageType.Reply)
        {
            UnblockAwait();
        }

        if (workflow.IsEndState)
        {
            return (null, false, true);
        }

        return (message.ToString(), message.MessageType is MessageType.Err or MessageType.Reply, false);
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