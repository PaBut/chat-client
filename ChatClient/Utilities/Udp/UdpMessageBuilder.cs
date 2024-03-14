using System.Text;
using ChatClient.Models;
using ChatClient.Utilities.Common;

namespace ChatClient.Utilities.Udp;

public class UdpMessageBuilder : IMessageBuilder
{
    public Message DecodeMessage(byte[] message)
    {
        if (message.Length < 1)
        {
            // TODO: Error processing
        }

        var messageType = UdpMessageTypeCoder.GetMessageType(message[0]);
        var arguments = new Dictionary<MessageArguments, object>();

        switch (messageType)
        {
            case MessageType.Confirm:
                if (message.Length < 3)
                {
                    // TODO: Validation error
                }

                arguments.Add(MessageArguments.ReferenceMessageId, BitConverter.ToUInt16(message[1..3]));
                break;
            case MessageType.Reply:
                if (message.Length < 8)
                {
                    // TODO: Validation error
                    return new Message()
                    {
                        MessageType = MessageType.Unknown
                    };
                }

                arguments.Add(MessageArguments.MessageId, BitConverter.ToUInt16(message[1..3]));

                arguments.Add(MessageArguments.ReplyStatus, message[3] == 1);

                arguments.Add(MessageArguments.ReferenceMessageId, BitConverter.ToUInt16(message[4..6]));

                var messageContentEnd1 = GetEndOfTheFloatingMessage(6, message);

                arguments.Add(MessageArguments.MessageContent, Encoding.UTF8.GetString(message[6..messageContentEnd1]));

                break;

            case MessageType.Err:
            case MessageType.Msg:
                if (message.Length < 7)
                {
                    // TODO: Validation error
                }

                arguments.Add(MessageArguments.MessageId, BitConverter.ToUInt16(message[1..3]));

                var displayNameEnd = GetEndOfTheFloatingMessage(3, message);

                arguments.Add(MessageArguments.DisplayName, Encoding.UTF8.GetString(message[3..displayNameEnd]));

                var messageContentEnd2 = GetEndOfTheFloatingMessage(displayNameEnd + 1, message);

                arguments.Add(MessageArguments.MessageContent,
                    Encoding.UTF8.GetString(message[displayNameEnd..messageContentEnd2]));

                break;
            case MessageType.Bye:
                if (message.Length < 3)
                {
                    // TODO: Validation error
                }

                arguments.Add(MessageArguments.ReferenceMessageId, BitConverter.ToInt16(message[1..3]));
                break;
        }

        return new Message()
        {
            MessageType = messageType,
            Arguments = arguments
        };
    }

    public byte[] GetByteMessage(Message message)
    {
        List<byte> byteMessage = new();

        byteMessage.Add(UdpMessageTypeCoder.GetMessageTypeCode(message.MessageType));

        if (message.MessageType == MessageType.Confirm)
        {
            byteMessage.AddRange(BitConverter.GetBytes((ushort)message.Arguments[MessageArguments.ReferenceMessageId]));

            return byteMessage.ToArray();
        }
        
        byteMessage.AddRange(BitConverter.GetBytes((ushort)message.Arguments[MessageArguments.MessageId]));
        
        switch (message.MessageType)
        {
            case MessageType.Err:
            case MessageType.Msg:
                byteMessage.AddRange(Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.DisplayName]));
                byteMessage.Add(0);
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.MessageContent]));
                byteMessage.Add(0);
                break;
            case MessageType.Auth:
                byteMessage.AddRange(Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.UserName]));
                byteMessage.Add(0);
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.DisplayName]));
                byteMessage.Add(0);
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.Secret]));
                byteMessage.Add(0);
                break;
            case MessageType.Join:
                byteMessage.AddRange(Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.ChannelId]));
                byteMessage.Add(0);
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.DisplayName]));
                byteMessage.Add(0);
                break;
            case MessageType.Bye:
                break;
            default:
                // TODO: Error not supported
                break;
        }
        
        return byteMessage.ToArray();
    }

    private int GetEndOfTheFloatingMessage(int start, byte[] message)
    {
        for (int i = start; i < message.Length; i++)
        {
            if (message[i] == 0x00)
            {
                return i;
            }
        }

        return -1;
    }
}