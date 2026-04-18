using GameLibrary.Models;

namespace DoodlesClient.Models;

public class ClientPlayer(PlayerData data, bool isCurrentClientPlayer)
{
    public PlayerData PlayerData { get; set; } = data;
    public bool IsCurrentClientPlayer { get; set; } = isCurrentClientPlayer;
}
