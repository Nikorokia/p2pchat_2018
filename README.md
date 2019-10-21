# Peer-to-Peer Chat in C#
Project 3 for CSCI 420: Computer Networking class at Houghton College, Spring 2018.

This project had a variety of options for exploration in network programming.  

I chose to explore peer-to-peer protocols and attempt to develop a basic protocol of my own to find nearby devices and initiate simple chat sessions.  

Multiple language options were available; I chose to continue studying C#.

## Project Scope
"4. Develop a peer-to-peer discovery or communication system.

- Make a protocol where programs running that protocol will find other machines running the same protocol.
- Use this discovery system to simplify configuration of some service. For example, have some machines offering a "printing" service and have other machines list available services.
- Must be restricted to running on our lab machines.
- Examples: Universal Plug and Play, Zeroconf, Bonjour, and Gossip protocol "

## Project Submission

### Building

To compile, from within the source base folder:
```
$ dotnet publish -c release
```

### Running

From within the release folder:
```
$ dotnet p2pchat.dll
```

Both computers should be on the same network with TCP and UDP enabled.

### Known Deficiencies

_As of project submission_

The chat client acting as a server (the computer that "chats second"), tries to close a C#-library TCPListener when the two clients finish talking and disconnects. The TCPListener does not close quickly, and may throw an error that it is not ready if a new connection is attempted shortly after the old one is closed.
