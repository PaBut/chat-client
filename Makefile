all:
	 dotnet publish -r linux-x64 --self-contained false /p:PublishSingleFile=true
	 cp ChatClient/bin/Release/net8.0/linux-x64/publish/ChatClient ipk24chat-client