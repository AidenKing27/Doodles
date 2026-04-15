namespace GameLibrary.Models;

public class PlayerData
{
    public string Username { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public int Score { get; set; }

    public PlayerData()
    {
        
    }

    public PlayerData(string username, string roomCode)
    {
        Username = username;
        RoomCode = roomCode;
    }
}
