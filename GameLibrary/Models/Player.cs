using System.Net.Sockets;

namespace GameLibrary.Models;

public class Player
{
    public TcpClient Client { get; set; }
    public PlayerData PlayerData { get; set; }

    public Player(TcpClient client, PlayerData data)
    {
        Client = client;
        PlayerData = data;
    }
}
