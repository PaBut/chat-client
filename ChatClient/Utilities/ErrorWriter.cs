namespace ChatClient.Utilities;

public class ErrorWriter
{
    private readonly TextWriter writer;

    public ErrorWriter(TextWriter writer)
    {
        this.writer = writer;
    }
    
    public void WriteError(string message)
    {
        writer.WriteLine($"ERROR: {message}");
    }
}