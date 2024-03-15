using System.Text;
using ChatClient.Models;

namespace ChatClient.Utilities.Tcp;

public class TcpMessageQueue
{
    private const string CLRF = "\r\n";
    private readonly Queue<Message> messageQueue = new();
    private readonly TcpMessageBuilder messageBuilder;

    public TcpMessageQueue(TcpMessageBuilder messageBuilder)
    {
        this.messageBuilder = messageBuilder;
    }

    public void Enqueue(byte[] encodedMessage)
    {
        var messageParts = Encoding.UTF8.GetString(encodedMessage).Split(CLRF)
            .Where(part => !string.IsNullOrEmpty(part));
        foreach (var part in messageParts)
        {
            var message = messageBuilder.DecodeMessage(part);
            messageQueue.Enqueue(message);
        }
    }

    public Message? Dequeue()
    {
        if (messageQueue.Count == 0)
        {
            return null;
        }

        return messageQueue.Dequeue();
    }
}