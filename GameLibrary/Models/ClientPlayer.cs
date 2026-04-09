namespace GameLibrary.Models;

public class ClientPlayer
{
    public PlayerData PlayerData { get; set; }
    public bool IsPlayer { get; set; }

    public ClientPlayer(PlayerData data, bool isPlayer)
    {
        PlayerData = data;
        IsPlayer = isPlayer;
    }
}
