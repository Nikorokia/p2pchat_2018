p2pchat readme

Running, from within the release folder:
p2pchat_release$ dotnet p2pchat.dll

Both computers should be on the same network with TCP and UDP enabled.

Known Deficiencies:
  Currently the chat client acting as a server (the computer that "chats second"), tries to close a C#-library TCPListener when the two clients finish talking and disconnects. The TCPListener does not close quickly, and may throw an error that it is not ready if a new connection is attempted shortly after the old one is closed.