using ChatClient.Models;

namespace ChatClient;

public interface IIpkClient : IDisposable
{
    Task SendMessage(Message message);
    Task Authenticate(Message message);
    Task JoinChannel(Message message);
    Task SendError(Message message);
    Task Leave();
    Task<Message> Listen();
}