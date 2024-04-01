# IPK Project 1

A client for a chat server, that allows users to communicate with over udp or tcp protocols.

## Usage 

### Compilation

```bash
make
```
Executable ```ipk24chat-client``` will be created in the root of the project
### Commandline options

* ```-t``` - Transport protocol used for connection <span style="color:red;font-weight:bold">(mandatory argument)</span>
* ```-s``` - Server IP or hostname <span style="color:red;font-weight:bold">(mandatory argument)</span>
* ```-p``` - Server port (default is 4567)
* ```-d``` - UDP confirmation timeout (default is 250)
* ```-r``` - Maximum number of UDP retransmissions (default is 3)
* ```-h``` - Prints program help output

Transport protocol can be either ```udp``` or ```tcp```


### Example of usage

```bash
./ipk24chat-client -t udp -s localhost -p 4000 -d 300
```

Connects to server using udp protocol on localhost with port 4000 and timeout for udp confimation 300 ms

### Program commands

```bash
/auth {Username} {Secret} {DisplayName} 
```
Authenticates the user using Username and Secret and locally sets DisplayName for future interraction with server

```bash
/join {ChannelId} 
```
User joins selected channel

```bash
/help
```
Prints help output of supported commands and its arguments 

```bash
{MessageContent}
```
Sends written message to the server 

## Implementation

The projected was implemented in C#.

### Program entrypoint

Program entrypoint is located in Program class. Commandline arguments are processed there and based on them using factory class ```IpkClientFactory``` desired socket client is created, which is then being used by ```WrappedIpkClient```. In addition to it listener and sender threads are created in Program class for respective purposes for communication with the server.

### ```WrappedIpkClient```

```WrappedIpkClient``` is the core of the application. User commands are being parsed, validated and processed there based on the current state of client(managed by ```WorkflowGraph```). If the command is used for interracting with the server, then it will be converted to ```Message``` object. After the conversion it is being validated with ```MessageValidator```. Then based on current state either message is processed by socket clients or user is being informed of an invalid state for such a message. It also uses respective socket client for listening and processing retrieved messages, if those are invalid error message is sent to the server. ```WrappedIpkClient``` uses ```IIpkClient``` interface for interracting with socket clients.

### ```IpkTcpClient```

Socket client for communication with server over tcp protocol, implements ```IIpkClient```. It uses ```TcpMessageCoder``` for decoding and encoding messages to Message object. It's also making a use of ```TcpMessageQueue``` in case of a couple of messages are sent in one request. There's also a proxy ```ITcpNetworkWriterProxy``` between ```NetworkStream``` of ```TcpClient``` from sdk and ```IpkTcpClient``` for mocking in testing purposes.


### ```IpkUdpClient```

Socket client for communication with server over udp protocol, implements ```IIpkClient```. Unlike in tcp protocol, in udp protocol it is mandatory to check if the opposing side received properly the request, that is why confirmation messages are needed to be sent. While sending messages to server, ```IpkUdpClient``` waits specific time given in timeout CLI argument before sending another request. That repeats for total retransmissions times + 1(initial one), that is also give in CLI arguments. For encoding and decoding messages to ```Message``` object, ```UdpMessageCode``` is used. Just like in ```IpkTcpClient``` there's also a proxy for testing purposes, in this case that is ```UdpClientProxy```.

<br>

Both listening methods of each socket client returns wrap for the ```Message``` object ```ResponseResult```. It contains information if the message is valid or already processed.


### ```WorkflowGraph```
State management is managed by ```WorkflowGraph```, which has information on the current state of an application, based on processed message transition to another state is being held. It also contains all the allowed messages for each state. State validation of processed messages is done by that.

## Testing

For testing purposes reference server and testing project were used. Basically already mentioned proxies are mocked in testing project, that simulates interraction with the server, but instead of sending message to server, they are stores in respective queues. The testing project is implemented with xUnit and Moq. 

The core of the project ```WrappedIpkClient``` is being tested there. Any possible commands and some of its edgecases are tested there. Mostly messages sent to server, client's reaction on different requests are checked. The tests are present in the repository together with the project.

Tests were mainly implemented for regression testing and for functionality check. The only thing is, they are need to be ran sequentially.