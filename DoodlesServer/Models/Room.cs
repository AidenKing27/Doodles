using GameLibrary.Models;
using System.Collections.Concurrent;
using System.IO;

namespace DoodlesServer.Models;

public class Room(ServerPlayer host, string code, int playerCount, int doodleTime, int rounds, List<ServerPlayer> players)
{
    public delegate Task RoomLetterHandler(Room room, char letter, int index);
    public event RoomLetterHandler? RevealLetterEvent;

    public delegate Task RoomRoundHandler(Room room);
    public event RoomRoundHandler? EndRoundEvent;

    private const int MAX_POINTS = 300;
    private int _timeRemaining;

    public ServerPlayer Host { get; set; } = host;
    public string Code { get; set; } = code;
    public int MaxPlayerCount { get; set; } = playerCount;
    public int DoodleTime { get; set; } = doodleTime;
    public int Rounds { get; set; } = rounds;
    public bool IsGameStarted { get; set; }
    public bool IsRoundStarted { get; set; }
    public List<ServerPlayer> Players { get; set; } = players;
    public Queue<ServerPlayer> PlayerQueue { get; set; } = [];
    public ConcurrentQueue<DoodleInfo> DoodleQueue { get; set; } = [];
    public string CurrentWord { get; set; } = string.Empty;
    public List<string> AllUsedWords { get; set; } = [];
    public List<PlayerData> AllGuessedPlayers { get; set; } = [];

    public List<string> GetThreeRandomWords()
    {
        List<string> words = [];

        var lines = File.ReadLines(@"../../../Files/wordlist.txt");
        Random rnd = new();
        while (words.Count < 3)
        {
            string word = lines.ElementAt(rnd.Next(lines.Count()));
            if (!AllUsedWords.Contains(word))
            {
                words.Add(word);
                AllUsedWords.Add(word);
            }
        }

        return words;
    }

    public int GetGuessScore()
    {
        double ratio = _timeRemaining / DoodleTime;
        return (int)Math.Round(MAX_POINTS * ratio);
    }

    public Dictionary<Guid, PlayerRankPair> GetRankOrder()
    {
        var sortedPlayersByScore = Players
            .OrderByDescending(p => p.PlayerData.Score)
            .ToList();

        Dictionary<Guid, PlayerRankPair> rankingDictionary = [];
        for (int i = 0; i < sortedPlayersByScore.Count; i++)
            rankingDictionary.Add(sortedPlayersByScore[i].PlayerData.GUID, new PlayerRankPair(i + 1, sortedPlayersByScore[i].PlayerData.Score));

        return rankingDictionary;
    }

    public async Task RunRoundAsync(CancellationToken cancellationToken = default)
    {
        AllGuessedPlayers.Clear();

        IsRoundStarted = true;
        _timeRemaining = DoodleTime;
        int revealDuration = Math.Max(1, DoodleTime / 2);

        Dictionary<int, char> letterIndexes = [];
        for (int i = 0; i < CurrentWord.Length; i++)
        {
            if (CurrentWord[i] != ' ')
                letterIndexes.Add(i, CurrentWord[i]);
        }

        int revealCount = Math.Min(3, letterIndexes.Count / 2);
        Random rnd = new();

        while (_timeRemaining > 0 && AllGuessedPlayers.Count < Players.Count - 1)
        {
            int wait = Math.Min(revealDuration, _timeRemaining);
            await Task.Delay(TimeSpan.FromSeconds(wait), cancellationToken);
            _timeRemaining -= wait;
            revealDuration = Math.Max(1, revealDuration / 2);

            if (_timeRemaining > 0 && revealCount > 0 && letterIndexes.Count > 0)
            {
                int key = letterIndexes.Keys.ElementAt(rnd.Next(letterIndexes.Count));
                char letter = letterIndexes[key];
                letterIndexes.Remove(key);

                await InvokeRevealLetterEventAsync(letter, key);
                revealCount--;
            }
        }

        IsRoundStarted = false;
        await InvokeEndRoundEventAsync();
    }

    // AI: async event
    private async Task InvokeRevealLetterEventAsync(char letter, int index)
    {
        if (RevealLetterEvent is not { } handlers) return;

        foreach (RoomLetterHandler handler in handlers.GetInvocationList())
        {
            await handler(this, letter, index);
        }
    }

    // AI: async event
    private async Task InvokeEndRoundEventAsync()
    {
        if (EndRoundEvent is not { } handlers) return;

        foreach (RoomRoundHandler handler in handlers.GetInvocationList())
        {
            await handler(this);
        }
    }
}
