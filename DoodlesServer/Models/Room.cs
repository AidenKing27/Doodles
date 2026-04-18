using GameLibrary.Models;
using System.Collections.Concurrent;

namespace DoodlesServer.Models;

public class Room(ServerPlayer host, string code, int playerCount, int doodleTime, int rounds, List<ServerPlayer> players)
{
    public ServerPlayer Host { get; set; } = host;
    public string Code { get; set; } = code;
    public int PlayerCount { get; set; } = playerCount;
    public int DoodleTime { get; set; } = doodleTime;
    public int Rounds { get; set; } = rounds;
    public List<ServerPlayer> Players { get; set; } = players;
    public Queue<ServerPlayer> PlayerQueue { get; set; } = [];
    public ConcurrentQueue<DoodleInfo> DoodleQueue { get; set; } = [];
    public List<string> AllUsedWords { get; set; } = [];
}
