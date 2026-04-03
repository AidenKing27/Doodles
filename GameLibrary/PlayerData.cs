namespace GameLibrary;

public class PlayerData
{
    public string Username { get; set; }
    public string RoomCode { get; set; }
    public int Score { get; set; }

    public PlayerData(string username, string roomCode)
    {
        Username = username;
        RoomCode = roomCode;
    }
}
