using System.Text;
using ChatClient.Models;
using ChatClient.Utilities.Common;

namespace ChatClient.Utilities.Tcp;

public class TcpMessageBuilder : IMessageBuilder
{
    public byte[] GetByteMessage(Message message)
    {
        string messageString = TcpMessageTypeCoder.GetMessageString(message.MessageType);

        if (message.MessageType == MessageType.Bye)
        {
            return Encoding.UTF8.GetBytes(messageString);
        }

        messageString += " ";

        if (message.MessageType == MessageType.Join)
        {
            if (!message.Arguments.ContainsKey(MessageArguments.ChannelId) &&
                !message.Arguments.ContainsKey(MessageArguments.DisplayName))
            {
                // Error
            }

            messageString += (string)message.Arguments[MessageArguments.ChannelId] + " " +
                             (string)message.Arguments[MessageArguments.DisplayName];
        }
        else if (message.MessageType == MessageType.Auth)
        {
            if (!message.Arguments.ContainsKey(MessageArguments.UserName) &&
                !message.Arguments.ContainsKey(MessageArguments.DisplayName) &&
                !message.Arguments.ContainsKey(MessageArguments.Secret))
            {
                // Error
            }

            messageString += (string)message.Arguments[MessageArguments.UserName] + " " +
                             (string)message.Arguments[MessageArguments.DisplayName] + " USING " +
                             (string)message.Arguments[MessageArguments.Secret];
        }
        else if (message.MessageType == MessageType.Msg || message.MessageType == MessageType.Err)
        {
            if (!message.Arguments.ContainsKey(MessageArguments.DisplayName) &&
                !message.Arguments.ContainsKey(MessageArguments.MessageContent))
            {
                // Error
            }

            messageString += (string)message.Arguments[MessageArguments.DisplayName] + " IS " +
                             (string)message.Arguments[MessageArguments.MessageContent];
        }
        else if (message.MessageType == MessageType.Reply)
        {
            // Not supported processing
        }
        else
        {
            throw new Exception("Not supported");
        }

        return Encoding.UTF8.GetBytes(messageString);
    }

    public Message DecodeMessage(byte[] message)
    {
        string messageString = Encoding.UTF8.GetString(message);

        string[] messageParts = messageString.Split(" ");

        var stringMessageType = messageParts[1] == "FROM" ? string.Join(' ', messageParts[..2]) : messageParts[0];

        var messageType = TcpMessageTypeCoder.GetMessageType(stringMessageType);

        if (messageType == null)
        {
            throw new Exception("Invalid message type");
        }

        var messageArguments = new Dictionary<MessageArguments, object>();

        if (messageType == MessageType.Msg || messageType == MessageType.Err)
        {
            messageArguments.Add(MessageArguments.DisplayName, messageParts[2]);

            if (messageParts[3] != "IS")
            {
                // Invalid Message
                throw new Exception("Not supported");
            }
            
            messageArguments.Add(MessageArguments.MessageContent, messageParts[4]);
        }
        else if (messageType == MessageType.Reply)
        {
            if(messageParts[1] != "OK" && messageParts[1] != "NOK")
            {
                // Invalid Message
                throw new Exception("Not supported");
            }
            
            messageArguments.Add(MessageArguments.ReplyStatus, messageParts[1] == "OK");
            
            if (messageParts[2] != "IS")
            {
                // Invalid Message
                throw new Exception("Not supported");
            }
            
            messageArguments.Add(MessageArguments.MessageContent, messageParts[3]);
        }
        else if (messageType != MessageType.Bye)
        {
            throw new Exception("Not supported");
        }

        return new Message
        {
            MessageType = messageType.Value,
            Arguments = messageArguments
        };
    }
}