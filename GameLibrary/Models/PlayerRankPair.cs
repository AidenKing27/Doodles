namespace GameLibrary.Models;

public class PlayerRankPair(int rank, int score)
{
    public int Rank { get; set; } = rank;
    public int Score { get; set; } = score;
}
