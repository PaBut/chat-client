using ChatClient.ServerClients;
using ChatClient.Tests.Initializers;
using ChatClient.Tests.Utilities;
using Xunit.Abstractions;

namespace ChatClient.Tests;

public class UdpTests
{
    private const byte retrials = 3;
    private const ushort timeout = 500;

    private readonly ITestOutputHelper output;
    private readonly WrappedIpkClient wrappedClient;
    private readonly MessageQueueManager messageQueueManager = new();
    private readonly ErrorQueueManager errorQueueManager = new();

    public UdpTests(ITestOutputHelper output)
    {
        this.output = output;
        wrappedClient =
            WrappedClientInitializers.GetUdpWrappedClient(messageQueueManager, errorQueueManager,
                retrials, timeout);
    }

    private static bool ArraysAreIdentical<T>(T[] array1, T[] array2) where T : IEquatable<T>
    {
        if (array1.Length != array2.Length)
            return false;

        for (int i = 0; i < array1.Length; i++)
        {
            if (!array1[i].Equals(array2[i]))
                return false;
        }

        return true;
    }

    [Fact]
    public async Task Authentication_OK()
    {
        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/auth k km kmm"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        var expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(2);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)0));
        expectedByteMessage.AddRange("k"u8.ToArray());
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange("kmm"u8.ToArray());
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange("km"u8.ToArray());
        expectedByteMessage.Add(0);

        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
        messageQueueManager.SendMessageToClient((new[] { (byte)0 }).Concat(BitConverter.GetBytes((ushort)0)).ToArray());
        var response = await wrappedClient.Listen();
        Assert.Null(response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.False(response.Value.IsServerError);
        Assert.False(response.Value.ToStderr);
        messageQueueManager.SendMessageToClient((new[] { (byte)1 }).Concat(BitConverter.GetBytes((ushort)10))
            .Concat(new[] { (byte)1 }).Concat(BitConverter.GetBytes((ushort)0))
            .Concat("Authentication successful"u8.ToArray()).Concat(new[] { (byte)0 }).ToArray());
        response = await wrappedClient.Listen();

        await sender;

        Assert.NotNull(response);
        Assert.Equal("Success: Authentication successful", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);

        message = messageQueueManager.GetSentMessage();
        expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)10));
        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
    }

    [Fact]
    public async Task Authentication_NOK()
    {
        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/auth k km kmm"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        var expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(2);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)0));
        expectedByteMessage.AddRange("k"u8.ToArray());
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange("kmm"u8.ToArray());
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange("km"u8.ToArray());
        expectedByteMessage.Add(0);

        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
        messageQueueManager.SendMessageToClient((new[] { (byte)0 }).Concat(BitConverter.GetBytes((ushort)0)).ToArray());
        var response = await wrappedClient.Listen();
        Assert.Null(response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.False(response.Value.IsServerError);
        Assert.False(response.Value.ToStderr);
        messageQueueManager.SendMessageToClient((new[] { (byte)1 }).Concat(BitConverter.GetBytes((ushort)10))
            .Concat(new[] { (byte)0 }).Concat(BitConverter.GetBytes((ushort)0))
            .Concat("Authentication failed"u8.ToArray()).Concat(new[] { (byte)0 }).ToArray());
        response = await wrappedClient.Listen();

        await sender;

        Assert.NotNull(response);
        Assert.Equal("Failure: Authentication failed", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);

        message = messageQueueManager.GetSentMessage();
        expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)10));
        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
    }

    [Fact]
    public async Task DoubleAuthentication_OK()
    {
        await Authentication_NOK();

        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/auth k km kmm"); });

        await Task.Delay(200);
        var message = messageQueueManager.GetSentMessage();
        var expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(2);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)1));
        expectedByteMessage.AddRange("k"u8.ToArray());
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange("kmm"u8.ToArray());
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange("km"u8.ToArray());
        expectedByteMessage.Add(0);

        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
        messageQueueManager.SendMessageToClient((new[] { (byte)0 }).Concat(BitConverter.GetBytes((ushort)2)).ToArray());
        var response = await wrappedClient.Listen();
        Assert.Null(response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.False(response.Value.IsServerError);
        Assert.False(response.Value.ToStderr);
        messageQueueManager.SendMessageToClient((new[] { (byte)1 }).Concat(BitConverter.GetBytes((ushort)12))
            .Concat(new[] { (byte)1 }).Concat(BitConverter.GetBytes((ushort)1))
            .Concat("Authentication successful"u8.ToArray()).Concat(new[] { (byte)0 }).ToArray());
        response = await wrappedClient.Listen();

        await sender;

        Assert.NotNull(response);
        Assert.Equal("Success: Authentication successful", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);

        message = messageQueueManager.GetSentMessage();
        expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)12));
        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
    }

    [Fact]
    public async Task SendMessage()
    {
        await Authentication_OK();

        await wrappedClient.RunCommand("he  llo");

        var message = messageQueueManager.GetSentMessage();

        Assert.True(ArraysAreIdentical(message,
            (new[] { (byte)4 }).Concat(BitConverter.GetBytes((ushort)1).Concat("kmm"u8.ToArray())
                .Concat(new[] { (byte)0 }).Concat("he  llo"u8.ToArray()).Concat(new[] { (byte)0 })).ToArray()));
    }

    [Fact]
    public async Task SendMessageAfterRename()
    {
        await Authentication_OK();

        await wrappedClient.RunCommand("/rename tom");
        await wrappedClient.RunCommand("hello");

        var message = messageQueueManager.GetSentMessage();

        Assert.True(ArraysAreIdentical(message,
            (new[] { (byte)4 }).Concat(BitConverter.GetBytes((ushort)1).Concat("tom"u8.ToArray())
                .Concat(new[] { (byte)0 }).Concat("hello"u8.ToArray()).Concat(new[] { (byte)0 })).ToArray()));
    }

    [Fact]
    public async Task ReceiveMessage()
    {
        await Authentication_OK();

        messageQueueManager.SendMessageToClient((new[] { (byte)4 }).Concat(BitConverter.GetBytes((ushort)1).Concat("peter"u8.ToArray())
            .Concat(new[] { (byte)0 }).Concat("supp p"u8.ToArray()).Concat(new[] { (byte)0 })).ToArray());
        var response = await wrappedClient.Listen();

        Assert.NotNull(response);
        Assert.Equal("peter: supp p", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.False(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);
        
        var message = messageQueueManager.GetSentMessage();
        var expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)1));
        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
    }

    [Fact]
    public async Task JoinChannel_NOK()
    {
        await Authentication_OK();

        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/join channel-1"); });

        await Task.Delay(200);
        
        var message = messageQueueManager.GetSentMessage();
        var expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(3);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)1));
        expectedByteMessage.AddRange("channel-1"u8.ToArray());
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange("kmm"u8.ToArray());
        expectedByteMessage.Add(0);

        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
        
        messageQueueManager.SendMessageToClient((new[] { (byte)0 }).Concat(BitConverter.GetBytes((ushort)1)).ToArray());
        var response = await wrappedClient.Listen();
        Assert.Null(response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.False(response.Value.IsServerError);
        Assert.False(response.Value.ToStderr);
        messageQueueManager.SendMessageToClient((new[] { (byte)1 }).Concat(BitConverter.GetBytes((ushort)15))
            .Concat(new[] { (byte)0 }).Concat(BitConverter.GetBytes((ushort)1))
            .Concat("user couldnt join new channel"u8.ToArray()).Concat(new[] { (byte)0 }).ToArray());
        
        response = await wrappedClient.Listen();
        
        Assert.NotNull(response);
        Assert.Equal("Failure: user couldnt join new channel", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);

        message = messageQueueManager.GetSentMessage();
        expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)15));
        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
    }

    [Fact]
    public async Task JoinChannel_OK()
    {
        await Authentication_OK();

        var sender = Task.Run(async () => { await wrappedClient.RunCommand("/join channel-1"); });

        await Task.Delay(200);
        
        var message = messageQueueManager.GetSentMessage();
        var expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(3);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)1));
        expectedByteMessage.AddRange("channel-1"u8.ToArray());
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange("kmm"u8.ToArray());
        expectedByteMessage.Add(0);

        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
        
        messageQueueManager.SendMessageToClient((new[] { (byte)0 }).Concat(BitConverter.GetBytes((ushort)1)).ToArray());
        var response = await wrappedClient.Listen();
        Assert.Null(response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.False(response.Value.IsServerError);
        Assert.False(response.Value.ToStderr);
        messageQueueManager.SendMessageToClient((new[] { (byte)1 }).Concat(BitConverter.GetBytes((ushort)15))
            .Concat(new[] { (byte)1 }).Concat(BitConverter.GetBytes((ushort)1))
            .Concat("user joined new channel"u8.ToArray()).Concat(new[] { (byte)0 }).ToArray());
        
        response = await wrappedClient.Listen();
        
        Assert.NotNull(response);
        Assert.Equal("Success: user joined new channel", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);

        message = messageQueueManager.GetSentMessage();
        expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)15));
        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
    }

    [Fact]
    public async Task ServerError()
    {
        await Authentication_OK();
        
        messageQueueManager.SendMessageToClient((new[] { (byte)0xFE }).Concat(BitConverter.GetBytes((ushort)1).Concat("Server"u8.ToArray())
            .Concat(new[] { (byte)0 }).Concat("idk what happened here"u8.ToArray()).Concat(new[] { (byte)0 })).ToArray());
        var response = await wrappedClient.Listen();

        Assert.NotNull(response);
        Assert.Equal("ERR FROM Server: idk what happened here", response.Value.Message);
        Assert.False(response.Value.ByeReceived);
        Assert.True(response.Value.ToStderr);
        Assert.True(response.Value.IsServerError);
        
        var message = messageQueueManager.GetSentMessage();
        var expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)1));
        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
    }

    [Fact]
    public async Task ByeReceived()
    {
        await Authentication_OK();
        
        messageQueueManager.SendMessageToClient((new[] { (byte)0xFF }).Concat(BitConverter.GetBytes((ushort)1)).ToArray());
        var response = await wrappedClient.Listen();

        Assert.NotNull(response);
        Assert.Null(response.Value.Message);
        Assert.True(response.Value.ByeReceived);
        Assert.False(response.Value.ToStderr);
        Assert.False(response.Value.IsServerError);
        
        var message = messageQueueManager.GetSentMessage();
        var expectedByteMessage = new List<byte>();
        expectedByteMessage.Add(0);
        expectedByteMessage.AddRange(BitConverter.GetBytes((ushort)1));
        Assert.True(ArraysAreIdentical(expectedByteMessage.ToArray(), message));
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