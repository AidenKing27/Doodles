namespace GameLibrary.Models;

public class PlayerData(string username, string roomCode, bool isHost)
{
    public string Username { get; set; } = username;
    public string RoomCode { get; set; } = roomCode;
    public int Score { get; set; }
    public int Place { get; set; }
    public bool IsJoined { get; set; }
    public bool IsTurn { get; set; }
    public bool IsHost { get; set; } = isHost;

    public PlayerData() : this(string.Empty, string.Empty, false) { }
}
