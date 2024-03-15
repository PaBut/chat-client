namespace ChatClient.Models;

public class Message
{
    public MessageType MessageType { get; set; }
    public IDictionary<MessageArguments, object> Arguments { get; set; }
    public bool IsUdp { get; set; } = false;

    public static Message? FromCommandLine(string line, out string? errorResponse)
    {
        var unknownMessage = new Message()
        {
            MessageType = MessageType.Unknown,
            Arguments = new Dictionary<MessageArguments, object>()
        };
        errorResponse = null;
        
        if (line[0] != '/')
        {
            return new Message()
            {
                MessageType = MessageType.Msg,
                Arguments = new Dictionary<MessageArguments, object>()
                {
                    { MessageArguments.MessageContent, line }
                }
            };
        }
        
        var parts = line.Split(' ');
        var command = parts[0];
        
        if (command == "/auth")
        {
            if (parts.Length != 4)
            {
                return unknownMessage;
            }
            
            return new Message()
            {
                MessageType = MessageType.Auth,
                Arguments = new Dictionary<MessageArguments, object>()
                {
                    { MessageArguments.UserName, parts[1] },
                    { MessageArguments.Secret, parts[2] },
                    { MessageArguments.DisplayName, parts[3] },
                }
            };
        }
        else if (command == "/join")
        {
            if(parts.Length != 2)
            {
                return unknownMessage;
            }
            
            return new Message()
            {
                MessageType = MessageType.Join,
                Arguments = new Dictionary<MessageArguments, object>()
                {
                    { MessageArguments.ChannelId, parts[1] },
                }
            };
        }

        return unknownMessage;
    }

    public override string? ToString()
    {
        if(MessageType == MessageType.Msg)
        {
            return $"{(string) Arguments[MessageArguments.DisplayName]}: " +
                   $"{(string) Arguments[MessageArguments.MessageContent]}";
        }

        if (MessageType == MessageType.Err)
        {
            return $"ERROR FROM {(string) Arguments[MessageArguments.DisplayName]}: " +
                   $"{(string) Arguments[MessageArguments.MessageContent]}";
        }

        if (MessageType == MessageType.Reply)
        {
            var success = (bool) Arguments[MessageArguments.ReplyStatus] ? "SUCCESS" : "ERROR";
            return $"{success}: {(string) Arguments[MessageArguments.MessageContent]}";
        }

        return null;
    }
    
    public static Message UnknownMessage => new()
    {
        MessageType = MessageType.Unknown,
        Arguments = new Dictionary<MessageArguments, object>()
    };
}