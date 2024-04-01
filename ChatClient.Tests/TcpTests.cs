using ChatClient.ServerClients;
using ChatClient.Tests.Initializers;
using ChatClient.Tests.Utilities;
using Xunit.Abstractions;

namespace ChatClient.Tests;

public class TcpTests
{
    private readonly ITestOutputHelper output;
    private readonly WrappedIpkClient wrappedClient;
    private readonly MessageQueueManager messageQueueManager = new();
    private readonly ErrorQueueManager errorQueueManager = new();

    public TcpTests(ITestOutputHelper output)
    {
        this.output = output;
        wrappedClient = WrappedClientInitializers.GetTcpWrappedClient(messageQueueManager, errorQueueManager);
    }

    [Fact]
    public async Task Authentication_OK()
    {
        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/auth k k k"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        Assert.Equal("AUTH k AS k USING k\r\n"u8.ToArray(), message);
        messageQueueManager.SendMessageToClient("REPLY OK IS good\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();
        await sender;
        
        Assert.NotNull(response);
        Assert.Equal("Success: good", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError); 
    }
    
    [Fact]
    public async Task Authentication_NOK()
    {
        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/auth k k k"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        Assert.Equal("AUTH k AS k USING k\r\n"u8.ToArray(), message);
        messageQueueManager.SendMessageToClient("REPLY NOK IS bad\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();

        await sender;
        Assert.NotNull(response);
        Assert.Equal("Failure: bad", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);
    }
    
    [Fact]
    public async Task DoubleAuthentication_OK()
    {
        await Authentication_NOK();
        
        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/auth k k k"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        Assert.Equal("AUTH k AS k USING k\r\n"u8.ToArray(), message);
        messageQueueManager.SendMessageToClient("REPLY OK IS good\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();
        await sender;
        
        Assert.NotNull(response);
        Assert.Equal("Success: good", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError); 
    }
    
    [Fact]
    public async Task SendMessage()
    {
        await Authentication_OK();
        
        await wrappedClient.RunCommand("hello");

        var message = messageQueueManager.GetSentMessage();
        Assert.Equal("MSG FROM k IS hello\r\n"u8.ToArray(), message);
    }
    
    [Fact]
    public async Task SendMessageAfterRename()
    {
        await Authentication_OK();
        
        await wrappedClient.RunCommand("/rename tom");
        await wrappedClient.RunCommand("hello");

        var message = messageQueueManager.GetSentMessage();
        Assert.Equal("MSG FROM tom IS hello\r\n"u8.ToArray(), message);
    }
    
    [Fact]
    public async Task ReceiveMessage()
    {
        await Authentication_OK();
        
        messageQueueManager.SendMessageToClient("MSG FROM tomas IS supp p\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();

        Assert.NotNull(response);
        Assert.Equal("tomas: supp p", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.False(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError); 
    }
    
    [Fact]
    public async Task JoinChannel_NOK()
    {
        await Authentication_OK();
        
        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/join channel-1"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        Assert.Equal("JOIN channel-1 AS k\r\n"u8.ToArray(), message);
        messageQueueManager.SendMessageToClient("REPLY NOK IS bad response\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();

        await sender;
        Assert.NotNull(response);
        Assert.Equal("Failure: bad response", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);
    }
    
    [Fact]
    public async Task JoinChannel_OK()
    {
        await Authentication_OK();
        
        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/join channel-1"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        Assert.Equal("JOIN channel-1 AS k\r\n"u8.ToArray(), message);
        messageQueueManager.SendMessageToClient("REPLY OK IS good response\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();

        await sender;
        Assert.NotNull(response);
        Assert.Equal("Success: good response", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);
    }
    
    [Fact]
    public async Task ServerError()
    {
        await Authentication_OK();
        
        messageQueueManager.SendMessageToClient("ERR FROM Server IS idk what happened here\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();

        Assert.NotNull(response);
        Assert.Equal("ERR FROM Server: idk what happened here", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.True(response.Value.IsServerError); 
    }
    
    [Fact]
    public async Task ByeReceived()
    {
        await Authentication_OK();
        
        messageQueueManager.SendMessageToClient("BYE\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();

        Assert.NotNull(response);
        Assert.Null(response.Value.Message);
        Assert.True(response.Value.ByeReceived);
        Assert.False(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError); 
    }
    
    [Fact]
    public async Task MultipleMessages()
    {
        await Authentication_OK();
        
        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/join channel-1"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        Assert.Equal("JOIN channel-1 AS k\r\n"u8.ToArray(), message);
        messageQueueManager.SendMessageToClient("REPLY OK IS good response\r\nMSG FROM Server IS k joined channel-1\r\n"u8.ToArray());
        var response = await wrappedClient.Listen();

        await sender;
        Assert.NotNull(response);
        Assert.Equal("Success: good response", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);
        
        response = await wrappedClient.Listen();

        await sender;
        Assert.NotNull(response);
        Assert.Equal("Server: k joined channel-1", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.False(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);
    }
    
    [Fact]
    public async Task WrongState()
    {
        await wrappedClient.RunCommand("/join channel-1");
        
        var error = errorQueueManager.GetError();
        
        Assert.Equal("ERR: ", error[..5]);
    }
    
    [Fact]
    public async Task WrongArgumentsCount()
    {
        await wrappedClient.RunCommand("/auth k");
        
        var error = errorQueueManager.GetError();
        
        Assert.Equal("ERR: ", error[..5]);
    }
}