namespace ChatClient.Exceptions;

public class NotReceivedConfirmException : Exception
{
    public NotReceivedConfirmException(string message) : base(message) { }
}