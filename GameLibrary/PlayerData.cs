namespace GameLibrary;

public class PlayerData
{
    public string Username { get; set; }
    public int Score { get; set; }
    public PlayerData(string username)
    {
        Username = username;
    }
}
