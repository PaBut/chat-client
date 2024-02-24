using ChatClient.Models;

namespace ChatClient.Utilities.Common;

public interface IMessageBuilder
{
    public Message DecodeMessage(byte[] message);
    public byte[] GetByteMessage(Message message);
}