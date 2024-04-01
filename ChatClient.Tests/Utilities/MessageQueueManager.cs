namespace ChatClient.Tests.Utilities;

public class MessageQueueManager
{
    private static Queue<byte[]> incomingMessageQueue = new();
    private static Queue<byte[]> outcomingMessageQueue = new();
    
    public void SendMessageToClient(byte[] message)
    {
        incomingMessageQueue.Enqueue(message);
    }
    
    public byte[] GetReceivedMessage()
    {
        return incomingMessageQueue.Dequeue();
    }
    
    public void SendMessageToServer(byte[] message)
    {
        outcomingMessageQueue.Enqueue(message);
    }
    
    public byte[] GetSentMessage()
    {
        return outcomingMessageQueue.Dequeue();
    }
}