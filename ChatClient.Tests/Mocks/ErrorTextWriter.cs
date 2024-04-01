using ChatClient.Tests.Utilities;
using ChatClient.Utilities;
using CommandLine;
using Moq;

namespace ChatClient.Tests.Mocks;

public static class ErrorTextWriter
{
    public static Mock<TextWriter> GetMock(ErrorQueueManager queueManager)
    {
        var mock = new Mock<TextWriter>();
        mock.Setup(x => x.WriteLine(It.IsAny<string>()))
            .Callback((string message) => queueManager.WriteError(message));
        return mock;
    }
}