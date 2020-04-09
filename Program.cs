/*** Fill these lines with your complete information.
 * Note: Incomplete information may result in FAIL.
 * Member 1: Tommy van Geest.
 * Member 2: Tim Bras.
 * Std Number 1: 0919804.
 * Std Number 2: 0885872.
 * Class: INF5X.
 ***/


using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json; 
using System.Threading;

namespace SocketServer
{
    public class ClientInfo
    {
        public string studentnr { get; set; }
        public string classname { get; set; }
        public int clientid { get; set; }
        public string teamname { get; set; }
        public string ip { get; set; }
        public string secret { get; set; }
        public string status { get; set; }
    }

    public class Message 
    // Specifies the message that get sent with every time there is communication
    {
        public const string welcome = "WELCOME";
        public const string stopCommunication = "COMC-STOP";
        public const string statusEnd = "STAT-STOP";
        public const string secret = "SECRET";
    }

    public class ConcurrentServer
    {
        public Socket listener;
        public IPEndPoint localEndPoint;
        public IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        public readonly int portNumber = 11111; 
        //port as described in the sample

        public String results = "";
        public LinkedList<ClientInfo> clients = new LinkedList<ClientInfo>();

        private Boolean stopCond = false;
        private int processingTime = 1000;
        private int listeningQueueSize = 250;
        private int threadsBusy = 0;

        public void prepareServer()
        {
            try
            {
                Console.WriteLine("[Server] is ready to start ...");
                // Establish the local endpoint
                localEndPoint = new IPEndPoint(ipAddress, portNumber);
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Console.Out.WriteLine("[Server] A socket is established ...");
                // associate a network address to the Server Socket. All clients must know this address
                listener.Bind(localEndPoint);
                // This is a non-blocking listen with max number of pending requests
                listener.Listen(listeningQueueSize);
                while (true)
                {
                    Console.WriteLine("[Server] Waiting connection ... ");
                    // Suspend while waiting for incoming connection 
                    Socket connection = listener.Accept();
                    Console.WriteLine("[Server] Accepted connection");
                    handleClient(connection);

                }

            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }
        public void handleClient(Socket connection)
        {
            byte[] bytes = new Byte[1024];
            String data = null;
            int numByte = 0;
            string replyMsg = "";
            bool stop;

            Console.WriteLine("[Server] Going to handle client");

            Thread t = new Thread(() =>
            {
                threadsBusy++;
                this.sendReply(connection, Message.welcome);

                stop = false;
                while (!stop)
                {
                    numByte = connection.Receive(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, numByte);
                    replyMsg = processMessage(data);
                    if (replyMsg.Equals(Message.stopCommunication))
                    {
                        stop = true;
                        threadsBusy--;
                        //connectedAmount--;
                        break;
                    }
                    else
                        this.sendReply(connection, replyMsg);
                }

            });
            Console.WriteLine("[Server] New Thread created");
            t.Start();
            Console.WriteLine("[Server] Thread started");
            Console.WriteLine("[Server] Amount of busy Threads:" + threadsBusy);
        }
        public string processMessage(String msg)
        {
            Thread.Sleep(processingTime);
            Console.WriteLine("[Server] received from the client -> {0} ", msg);
            string replyMsg = "";

            try
            {
                switch (msg)
                {
                    case Message.stopCommunication:
                        replyMsg = Message.stopCommunication;
                        break;
                    default:
                        ClientInfo c = JsonSerializer.Deserialize<ClientInfo>(msg.ToString());
                        clients.AddLast(c);
                        if (c.clientid == -1)
                        {
                            stopCond = true;
                            exportResults();
                        }
                        c.secret = c.studentnr + Message.secret;
                        c.status = Message.statusEnd;
                        replyMsg = JsonSerializer.Serialize<ClientInfo>(c);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Server] processMessage {0}", e.Message);
            }

            return replyMsg;
        }
        public void sendReply(Socket connection, string msg)
        {
            byte[] encodedMsg = Encoding.ASCII.GetBytes(msg);
            connection.Send(encodedMsg);
        }
        public void exportResults()
        // Prints all clients when the ProcessMessage is done with handling the clients
        {
            if (stopCond)
            {
                this.printClients();
            }
        }
        public void printClients()
        // Prints a list of all the clients handled by the server
        {
            string delimiter = " , ";
            Console.Out.WriteLine("[Server] This is the list of clients communicated");
            foreach (ClientInfo c in clients)
            {
                Console.WriteLine(c.classname + delimiter + c.studentnr + delimiter + c.clientid.ToString());
            }
            Console.Out.WriteLine("[Server] Number of handled clients: {0}", clients.Count);

            clients.Clear();
            stopCond = false;

        }

    }

    public class ServerSimulator { 
        public static void concurrentRun()
        {
            Console.Out.WriteLine("[Server] A concurrent version of the sequential server");
            ConcurrentServer serverConcurrent = new ConcurrentServer();
            serverConcurrent.prepareServer();
        }
    }
    class Program
    {
        // Main Method 
        static void Main(string[] args)
        {
            Console.Clear();
            ServerSimulator.concurrentRun();
        }

    }
}
