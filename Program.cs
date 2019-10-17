using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace p2pchat
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!\n");
            bool running = true;
            startUDPListener(Globals.udpPort);

            if (Globals.debugging) foreach (IPAddress add in Globals.localIPAddresses) Console.WriteLine("Local IP Address: "+add);

            while(running) {
                switch (mainmenu()) {
                    case 1:                                         //specified address
                        if(getIpAddressFromUser())
                            if(requestConnect(Globals.targetIpAddress, Globals.udpPort))
                                startChatClient(Globals.targetIpAddress, Globals.tcpPort);
                        break;
                    case 2:                                         //search
                        if (searchNearby())
                            if (requestConnect(Globals.targetIpAddress,Globals.udpPort))
                                startChatClient(Globals.targetIpAddress, Globals.tcpPort);
                        break;
                    case 0:                                         //exit
                    default:
                        Console.WriteLine("Exit option selected.");
                        running = false;
                        break;
                }
            }

            Console.WriteLine("Good bye!");
            System.Threading.Thread.Sleep(Globals.ThreadSleepTime);
            System.Environment.Exit(0);
        }

        public static int mainmenu() {
            int numberOfOptions = 2;                                //options available in the UI output below.
            int choice = 0;

            Console.Write(
                "\n\nMain Menu:\n" +
                "    What would you like to do?\n" +
                "    [1] Enter an IP to chat with.\n" +
                "    [2] Search for others nearby.\n" +
                "    [0] Exit\n" +
                " >>  "
            );                                                      //update numberOfOptions variable above with new options.
            string input = Console.ReadLine();

            Int32.TryParse(input, out choice);                      //check input
            if (choice < 0 || choice > numberOfOptions) choice = 0; //check range, 0=exit
            return choice;
        }

        public static bool checkMenuResponseFalse(string input) {
            bool response = false;
            int numberChoice = 0;
            if (input.ToUpper().Equals("N") ||
                    input.ToUpper().Equals("NO") ||
                    input.ToUpper().Equals("QUIT") ||
                    input.ToUpper().Equals("EXIT") || (
                        Int32.TryParse(input, out numberChoice) &&
                        numberChoice <= 0
                    )) {
                        response = true;
                    }
            return response;
        }

        public static bool searchNearby() {
            var searchSuccess = false;
            var keepSearching = true;
            int maxAttempts = 10;
            int attempts = 0;
            var reportedClients = new List<IPAddress>();                //for UI reporting and interaction


            while (keepSearching) {
                UdpClient client = new UdpClient();

                attempts = 0;                                           //refresh number of attempts
                Globals.discoveredUDPClients.Clear();                           //refresh discovered
                reportedClients = new List<IPAddress>();                //refresh valid available
                if(Globals.debugging) reportedClients.Add(IPAddress.Parse(Globals.defaultIPAddress));

                Console.WriteLine("\n\nSearching...\nDiscovered Clients:");
                while (attempts <= maxAttempts) {

                    IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, Globals.udpPort);
                    byte[] bytes = new udpMessage("SEARCH",1,0,"").getByteArray();
                    client.Send(bytes, bytes.Length, ip);

                    while (!Globals.discoveredUDPClients.IsEmpty) {
                        Tuple<IPAddress,udpMessage> currentItem;
                        if (Globals.discoveredUDPClients.TryDequeue(out currentItem)
                            && !reportedClients.Contains(currentItem.Item1)
                            && !Globals.localIPAddresses.Contains(currentItem.Item1)
                            && currentItem.Item2.command.Equals("SEARCH")) {
                                reportedClients.Add(currentItem.Item1);
                                Console.WriteLine("    [" +reportedClients.IndexOf(currentItem.Item1)+ "] " +currentItem.Item1);
                        }
                    }

                    Thread.Sleep(Globals.ThreadSleepTime);
                    attempts++;
                }
                client.Close();

                if (reportedClients.Count <= 0) Console.WriteLine("  ! No nearby clients found.");

                Console.Write("    Search again?\n >> ");
                var input = Console.ReadLine();
                if (checkMenuResponseFalse(input)) {
                        keepSearching = false;
                }
            }

            if (reportedClients.Count > 0) {
                int menuoptions = reportedClients.Count + 1;
                Console.WriteLine("\n\nChoose a client to connect to!");
                foreach (IPAddress item in reportedClients) {
                    Console.WriteLine("    [" +(reportedClients.IndexOf(item)+1)+ "] " +item);
                }
                Console.Write("    [0] exit\n >>  ");
                int selectedMenuOption = 0;
                if (!Int32.TryParse(Console.ReadLine(), out selectedMenuOption) || selectedMenuOption > menuoptions || selectedMenuOption < 1) selectedMenuOption = 0; //check for invalid selection
                else {
                    Globals.targetIpAddress = reportedClients[selectedMenuOption-1]; //subtract 1 to offset for zero being exit
                    Console.WriteLine("    Option {0} chosen, {1} set as target IP Address.", selectedMenuOption, Globals.targetIpAddress);
                    searchSuccess = true;
                }
            }

            return searchSuccess;
        }

        public static Boolean getIpAddressFromUser() {
            bool getSuccess = false;
            bool userDone = false;

            while (!userDone) {
                Console.WriteLine("\n\nEnter an IP Address\n     Your own IP addresses are:");
                foreach (IPAddress address in Globals.localIPAddresses) Console.WriteLine("    - "+address);
                Console.Write("\n    Please enter the IP address of the node you would like to connect to:\n >> ");
                var input = Console.ReadLine();
                try {
                    Globals.targetIpAddress = IPAddress.Parse(input);
                    Console.WriteLine(" :) IP Address accepted! Target IP set to "+Globals.targetIpAddress);
                    getSuccess = true;
                    userDone = true;
                }
                catch(ArgumentNullException e)
                {
                    Console.WriteLine(" !! ArgumentNullException caught!!!");
                    Console.WriteLine("    Source : " + e.Source);
                    Console.WriteLine("    Message : " + e.Message);
                }

                catch(FormatException e)
                {
                    Console.WriteLine(" !! FormatException caught!!!");
                    Console.WriteLine("    Source : " + e.Source);
                    Console.WriteLine("    Message : " + e.Message);
                }
                
                catch(Exception e)
                {
                    Console.WriteLine(" !! Exception caught!!!");
                    Console.WriteLine("    Source : " + e.Source);
                    Console.WriteLine("    Message : " + e.Message);
                }
                if(!getSuccess) {
                    Console.Write("\n    Would you like try again?\n >> ");
                    if(checkMenuResponseFalse(Console.ReadLine())) userDone = true;
                }
            }

            return getSuccess;
        }

        public static Boolean startUDPListener(int port) {
            var listenerIsRunning = false;

            Thread listener = new Thread( (portNumber) => {
                UdpClient UdpListener = new UdpClient(port);
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, port);
                string received_data;
                byte[] receive_byte_array;
                try {
                    while (Globals.listenForUdp) {
                        if(Globals.UDPdebugging) Console.WriteLine("Waiting for broadcast");
                        receive_byte_array = UdpListener.Receive(ref groupEP);
                        if(Globals.UDPdebugging) Console.WriteLine("Received a broadcast from {0}", groupEP.ToString() );
                        received_data = Encoding.ASCII.GetString(receive_byte_array, 0, receive_byte_array.Length);
                        if(Globals.UDPdebugging) Console.WriteLine("data follows \n{0}\n\n", received_data);
                        Globals.discoveredUDPClients.Enqueue(new Tuple<IPAddress,udpMessage>(groupEP.Address,new udpMessage(receive_byte_array)));
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
                UdpListener.Close();
            });

            listener.Start(port);

            return listenerIsRunning;
        }

        public static Boolean requestConnect(IPAddress address, int port) {
            bool connectionSuccessful = false;
            bool userDone = false;

            UdpClient client = new UdpClient();
            Globals.discoveredUDPClients.Clear();                           //refresh discovered

            while (!userDone) {
                int attempts = 0;                                               //refresh number of attempts
                Console.WriteLine("\n\nRequesting connection to "+ address +":");
                while (attempts <= Globals.maxNetworkingAttempts && !connectionSuccessful) {
                    Console.WriteLine("    Attempt "+attempts+" of "+Globals.maxNetworkingAttempts+"...");

                    IPEndPoint ip = new IPEndPoint(address, port);
                    byte[] bytes = new udpMessage("CONNECT", 1, 0, "").getByteArray();
                    client.Send(bytes, bytes.Length, ip);

                    while (!Globals.discoveredUDPClients.IsEmpty) {
                        Tuple<IPAddress,udpMessage> currentItem;
                        if (Globals.discoveredUDPClients.TryDequeue(out currentItem)
                            && currentItem.Item1.Equals(address)
                            && currentItem.Item2.command.Equals("CONNECT")) {
                                connectionSuccessful = true;
                                userDone = true;
                        }
                    }

                    Thread.Sleep(Globals.ThreadSleepTime);
                    attempts++;
                }

                if(!connectionSuccessful) {
                    Console.Write("    There was no response; would you like to try again?\n >> ");
                    if(checkMenuResponseFalse(Console.ReadLine())) userDone = true;
                }
            }
            client.Close();

            if(connectionSuccessful) Console.WriteLine(" :) Connection approved!");
            else Console.WriteLine(" !! Connection Request Unsuccessful");

            return connectionSuccessful;
        }

        public static Boolean startChatClient(IPAddress ipAddress, int port) {
            bool chatClientSuccess = false, done = false, isWinner=false, winnerFound=false, firstTime = true;
            Console.WriteLine("\n\nStarting TCP Chat client...");

            winnerFound = udpPlayRockPaperScissors(ipAddress, Globals.udpPort, out isWinner);
            
            if (winnerFound) {
                if(isWinner) {
                    bool otherReady= false;

                    int attempts = 0;
                    while(attempts < Globals.maxNetworkingAttempts) {
                        while (!Globals.discoveredUDPClients.IsEmpty) {
                            Tuple<IPAddress,udpMessage> currentItem;
                            if (Globals.discoveredUDPClients.TryDequeue(out currentItem)
                                && currentItem.Item1.Equals(ipAddress)) {
                                    if(currentItem.Item2.command.Equals("TCPREADY")) {
                                        otherReady = true;
                                        attempts = Globals.maxNetworkingAttempts;
                                    }
                            }
                        }
                        attempts++;
                        Thread.Sleep(Globals.ThreadSleepTime);
                    }
                    if (otherReady) {
                        try {
                            TcpClient tcpClient = new TcpClient(ipAddress.ToString(), port);     
                            NetworkStream nwStream = tcpClient.GetStream();

                            string messageToSend ="";
                            while(!done) {
                                if(firstTime) { Console.Write("    Enter your message, type \"/exit\" when it's your turn to quit!\n >> "); firstTime = false; }
                                else Console.Write(" >> ");
                                messageToSend = Console.ReadLine();
                                if(messageToSend.Equals("/exit") || messageToSend.Equals("/quit") || messageToSend.Equals("/q")) {
                                    done = true;
                                    Byte[] dataToSend = System.Text.Encoding.ASCII.GetBytes(" !! The other user has left.");   
                                    nwStream.Write(dataToSend, 0, dataToSend.Length);
                                } else {
                                    Byte[] dataToSend = System.Text.Encoding.ASCII.GetBytes(messageToSend);   
                                    nwStream.Write(dataToSend, 0, dataToSend.Length);

                                    dataToSend = new Byte[256];

                                    // String to store the response ASCII representation.
                                    String responseData = String.Empty;

                                    // Read the first batch of the TcpServer response bytes.
                                    //Int32 bytes = nwStream.Read(data, 0, data.Length);
                                    //responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                                    //Console.WriteLine("    Received message:\n    ", responseData); 

                                    byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
                                    int bytesRead = nwStream.Read(buffer, 0, tcpClient.ReceiveBufferSize);
                                    string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                                    Console.WriteLine(" -- " + dataReceived);
                                }        
                            }
                            // Close everything.
                            nwStream.Close();         
                            tcpClient.Close();         
                        } 
                        catch (ArgumentNullException e) 
                        {
                            if(Globals.debugging) Console.WriteLine("ArgumentNullException: {0}", e);
                            Console.WriteLine(" !! Error; try again later!");
                        } 
                        catch (SocketException e) 
                        {
                            if(Globals.debugging) Console.WriteLine("SocketException: {0}", e);
                            Console.WriteLine(" !! Error, the socket was likely still in use; try again in a minute!");
                        }
                        catch (System.IO.IOException e) {
                            if(Globals.debugging) Console.WriteLine("IO Exception: "+e);
                            Console.WriteLine(" !! Error; the user likely left.");
                        }
                    }
                    else {
                        Console.WriteLine(" !! Could not connect to other client.");
                    }
                }

                else {
                    //---listen at the specified IP and port no.---
                    TcpListener listener = new TcpListener(IPAddress.Any, port);

                    Console.WriteLine("    The other person gets to chat first!");
                    try {
                        listener.Start();
                        
                        UdpClient udpClient = new UdpClient();
                        IPEndPoint ip = new IPEndPoint (ipAddress, Globals.udpPort);
                        byte[] udpBytes = new udpMessage("TCPREADY", 1, 1, "").getByteArray();
                        for(int i = 0; i < Globals.udpPulse; i++) udpClient.Send(udpBytes, udpBytes.Length, ip);
                        udpClient.Close();

                        TcpClient tcpClient = listener.AcceptTcpClient();

                        NetworkStream nwStream = tcpClient.GetStream();
                        byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
                        int bytesRead = nwStream.Read(buffer, 0, tcpClient.ReceiveBufferSize);
                        string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine(" -- " + dataReceived);

                        string messageToSend ="";
                        while(!done) {
                            if(firstTime) { Console.Write("    Enter your message, type \"/exit\" when it's your turn to quit!\n >> "); firstTime = false; }
                            else Console.Write(" >> ");
                            messageToSend = Console.ReadLine();
                            if(messageToSend.Equals("/exit") || messageToSend.Equals("/quit") || messageToSend.Equals("/q")) {
                                done = true;
                                Byte[] dataToSend = System.Text.Encoding.ASCII.GetBytes(" !! The other user has left.");   
                                nwStream.Write(dataToSend, 0, dataToSend.Length);
                            } else {
                                Byte[] dataToSend = System.Text.Encoding.ASCII.GetBytes(messageToSend);   
                                nwStream.Write(dataToSend, 0, dataToSend.Length);

                                dataToSend = new Byte[256];

                                String responseData = String.Empty;

                                buffer = new byte[tcpClient.ReceiveBufferSize];
                                bytesRead = nwStream.Read(buffer, 0, tcpClient.ReceiveBufferSize);
                                dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                                Console.WriteLine(" -- " + dataReceived);
                            }        
                        }
                        tcpClient.Close();
                        listener.Stop();
                    }
                    catch (ArgumentNullException e) 
                    {
                            if(Globals.debugging) Console.WriteLine("ArgumentNullException: {0}", e);
                            Console.WriteLine(" !! Error; try again later!");
                    } 
                    catch (SocketException e) 
                    {
                            if(Globals.debugging) Console.WriteLine("SocketException: {0}", e);
                            Console.WriteLine(" !! Error, the socket was likely still in use; try again in a minute!");
                    }
                    catch (System.IO.IOException e) {
                        if(Globals.debugging) Console.WriteLine("IO Exception: "+e);
                        Console.WriteLine(" !! Error; the user likely left.");
                    }
                }
            }
            Console.WriteLine("    Exiting the chat client");
            return chatClientSuccess;
        }
        public static Boolean waitForTcpServer() {
            return false;
        }

        public static Boolean udpPlayRockPaperScissors(IPAddress iPAddress, int port, out bool winner) {
            winner = false;
            Boolean winnerFound = false;
            if(Globals.rpsDebugging) Console.WriteLine("    Playing rock, paper, scissors!");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            UdpClient client = new UdpClient();
            //Globals.discoveredUDPClients.Clear();                           //refresh discovered
            int rpsThrow = new Random().Next(0, 10000);
            int currentMessageId = 333;

            while(!winnerFound) {
                if(Globals.rpsDebugging) Console.WriteLine(" :) Your throw: "+rpsThrow);
                IPEndPoint ip = new IPEndPoint (iPAddress, port);
                byte[] bytes = new udpMessage("RPS", currentMessageId, rpsThrow, "").getByteArray();
                for(int i = 0; i < Globals.udpPulse; i++) client.Send(bytes, bytes.Length, ip);
                Thread.Sleep(Globals.ThreadSleepTime);

                while (!Globals.discoveredUDPClients.IsEmpty) {
                    Tuple<IPAddress,udpMessage> currentItem;
                    udpMessage lastItem = new udpMessage("RPS", currentMessageId-1, 0, "");
                    if (Globals.discoveredUDPClients.TryDequeue(out currentItem)
                        && currentItem.Item1.Equals(iPAddress)) {
                            int otherThrow = currentItem.Item2.number;
                            if(Globals.rpsDebugging) Console.WriteLine("    Their throw: "+currentItem.Item2.number);
                            if(currentItem.Item2.messageId == currentMessageId) {
                                if (otherThrow > rpsThrow) {
                                    winnerFound=true;
                                    if(Globals.rpsDebugging) Console.WriteLine("    They won!");
                                }
                                else if (rpsThrow > otherThrow) {
                                    winner = true;
                                    winnerFound = true;
                                    if(Globals.rpsDebugging) Console.WriteLine(" :) You won!");
                                }
                                else {
                                    if (Globals.rpsDebugging) Console.WriteLine("    Tie? {0} from {1}", currentItem.Item2.number, currentItem.Item1);
                                }
                            }
                    }
                }
                if(stopwatch.ElapsedMilliseconds > Globals.maxRockPaperScissorsTime) {
                    Console.WriteLine(" !! Chat host could not be resolved.");
                    break;
                }
                Thread.Sleep(Globals.ThreadSleepTime);
            }
            client.Close();
            return winnerFound;
        }

    }

    public class udpMessage {
        private string delimiter="%&#";
        public string command;
        public int number, messageId;
        public string data;
        public udpMessage(string command, int messageId, int number, string data) {
            this.command = command;
            this.messageId = messageId;
            this.number = number;
            this.data = data;
        }
        public udpMessage(byte[] encodedmessage) {
            string[] message = Encoding.ASCII.GetString(encodedmessage).Split(delimiter);
            if (message.Length > 3) {
                command = message[0];
                if(!Int32.TryParse(message[1], out messageId)) number = -1;
                if(!Int32.TryParse(message[2], out number)) number = -1;
                data = message[3];
            }
            else {
                if(Globals.debugging) Console.WriteLine(" !! Received message was not in proper format");
                command = "";
                messageId = 0;
                number = -1;
                data="";
            }
        }
        public byte[] getByteArray() {
            return Encoding.ASCII.GetBytes(command+delimiter+messageId+delimiter+number+delimiter+data);
        }
    }

    public static class Globals {
        public static int udpPort = 60095, tcpPort = 60094; //port numbers
        public static int maxNetworkingAttempts = 10, maxRockPaperScissorsTime = 20000, ThreadSleepTime = 1000, udpPulse = 3;
        public static bool debugging = false, UDPdebugging = false, rpsDebugging = false; //debuggers
        public static bool listenForUdp = true;
        public static string defaultIPAddress = "192.168.1.1";
        public static IPAddress[] localIPAddresses = getAllIpAddresses();
        public static IPAddress targetIpAddress = IPAddress.Parse("0.0.0.0"); //default
        public static ConcurrentQueue<Tuple<IPAddress,udpMessage>> discoveredUDPClients = new ConcurrentQueue<Tuple<IPAddress,udpMessage>>();   //for the listener to report to

        private static IPAddress[] getAllIpAddresses() {
            var addressList = new List<IPAddress>();
            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties properties = netif.GetIPProperties();
                foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                {
                    addressList.Add(unicast.Address);
                }
            }
            return addressList.ToArray();
        }
    }
}
