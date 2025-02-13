using ChatClient.Models;

namespace ChatClient.SocketClients;

public interface IIpkClient : IDisposable
{
    Task SendMessage(Message message, CancellationToken cancellationToken = default);
    Task Authenticate(Message message, CancellationToken cancellationToken = default);
    Task JoinChannel(Message message, CancellationToken cancellationToken = default);
    Task SendError(Message message, CancellationToken cancellationToken = default);
    Task Leave();
    Task<ResponseResult> Listen(CancellationToken cancellationToken = default);
}