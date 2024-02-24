using ChatClient.Models;

namespace ChatClient;

public interface IIpkClient : IDisposable
{
    Task SendMessage(string message, string displayName);
    Task Authenticate(string userName, string secret, string displayName);
    Task JoinChannel(string channelId, string displayName);
    Task Leave();
    Task<Message> Listen();
}