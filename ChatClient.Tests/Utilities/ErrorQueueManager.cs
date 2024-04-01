namespace ChatClient.Tests.Utilities;

public class ErrorQueueManager
{
    private static Queue<string> errorQueue = new();
    
    public void WriteError(string message)
    {
        errorQueue.Enqueue(message);
    }
    
    public string GetError()
    {
        return errorQueue.Dequeue();
    }
}