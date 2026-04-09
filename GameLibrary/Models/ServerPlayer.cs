using System.Net.Sockets;

namespace GameLibrary.Models;

public class ServerPlayer
{
    public TcpClient Client { get; set; }
    public PlayerData PlayerData { get; set; }

    public ServerPlayer(TcpClient client, PlayerData data)
    {
        Client = client;
        PlayerData = data;
    }
}
