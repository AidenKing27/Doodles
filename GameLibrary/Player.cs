using System.Net.Sockets;

namespace GameLibrary;

public class Player
{
    public TcpClient Client { get; set; }
    public string Username { get; set; }

    public Player(TcpClient client, string username)
    {
        Client = client;
        Username = username;
    }
}
