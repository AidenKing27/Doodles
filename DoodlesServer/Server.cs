using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DoodlesServer;

public class Server
{
    private const string DELIM = "<!EOM!>";

    private TcpListener listener;
    private List<TcpClient> clients = new();
    private bool running;

    //make some eventsto handle server messages
    public delegate void ServerMessage(Packet280 packet);
    public event ServerMessage? ServerMessageEvent;

    //another delegate to interact with our GUI
    public delegate void LocalMessage(Packet280 packet);
    public event LocalMessage? LocalMessageEvent;

    public delegate void DisconnectMessage(string msg);
    public event DisconnectMessage? DisconnectMessageEvent;

    public Server(int port)
    {
        listener = new(IPAddress.Any, port);
    }
}
