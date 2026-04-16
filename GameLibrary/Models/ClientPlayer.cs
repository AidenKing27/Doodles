namespace GameLibrary.Models;

public class ClientPlayer
{
    public PlayerData PlayerData { get; set; }
    public bool IsCurrentClientPlayer { get; set; }

    public ClientPlayer(PlayerData data, bool isCurrentClientPlayer)
    {
        PlayerData = data;
        IsCurrentClientPlayer = isCurrentClientPlayer;
    }
}
