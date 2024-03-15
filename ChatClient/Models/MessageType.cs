namespace ChatClient.Models;

public enum MessageType
{
    Auth,
    Join,
    Reply, 
    Err,
    Bye,
    Msg,
    Confirm,
    Unknown,
}