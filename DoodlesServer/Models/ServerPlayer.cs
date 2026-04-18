using GameLibrary.Models;
using System.Net.Sockets;

namespace DoodlesServer.Models;

public class ServerPlayer(TcpClient client, PlayerData data)
{
    public TcpClient Client { get; set; } = client;
    public PlayerData PlayerData { get; set; } = data;
}
