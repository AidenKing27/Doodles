using GameLibrary.Models;
using System.Collections.Concurrent;
using System.IO;

namespace DoodlesServer.Models;

public class Room(ServerPlayer host, string code, int playerCount, int doodleTime, int maxRounds, List<ServerPlayer> players)
{

    public delegate Task RoomLetterHandler(Room room, char letter, int index);
    public event RoomLetterHandler? RevealLetterEvent;

    public delegate Task RoomRoundHandler(Room room);
    public event RoomRoundHandler? EndRoundEvent;

    private const int MAX_POINTS = 300;
    private int _timeRemaining;
    //private bool _endedByTime;
    private TaskCompletionSource<bool> _allGuessedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ServerPlayer Host { get; set; } = host;
    public string Code { get; set; } = code;
    public int MaxPlayerCount { get; set; } = playerCount;
    public int DoodleTime { get; set; } = doodleTime;
    public int MaxRounds { get; set; } = maxRounds;
    public bool IsGameStarted { get; set; }
    public bool IsRoundStarted { get; set; }
    public List<ServerPlayer> Players { get; set; } = players;
    public Queue<ServerPlayer> PlayerQueue { get; set; } = [];
    public ConcurrentQueue<DoodleInfo> DoodleQueue { get; set; } = [];
    public ConcurrentQueue<DoodleInfo> RoundDoodles { get; set; } = [];
    public ConcurrentQueue<RevealedLetters> RevealedLetters { get; set; } = [];
    public string CurrentWord { get; set; } = string.Empty;
    public List<string> AllUsedWords { get; set; } = [];
    public ConcurrentDictionary<Guid, PlayerData> AllGuessedPlayers { get; set; } = [];
    public Dictionary<Guid, int> RoundStartScores { get; set; } = [];
    public Guid CurrentTurnPlayerGuid { get; set; }
    public int RoundsPlayed { get; set; }
    public RoomPhase Phase { get; set; } = RoomPhase.Lobby;
    public bool EndedByTime { get; set; }

    public void BeginRound()
    {
        CurrentWord = string.Empty;
        IsRoundStarted = false;
        Phase = RoomPhase.ChoosingWord;

        DoodleQueue.Clear();
        RoundDoodles.Clear();
        RevealedLetters.Clear();
    }

    public void BeginDoodling(string chosenWord)
    {
        CurrentWord = chosenWord;
        IsRoundStarted = true;
        Phase = RoomPhase.Drawing;

        DoodleQueue.Clear();
        RoundDoodles.Clear();
        RevealedLetters.Clear();
    }

    public void TryCompleteRoundIfAllGuessed()
    {
        int requiredGuessers = Players.Count(p => p.PlayerData.GUID != CurrentTurnPlayerGuid);
        if (AllGuessedPlayers.Count >= requiredGuessers)
            _allGuessedTcs.TrySetResult(true);
        EndedByTime = false;
    }

    public List<string> GetThreeRandomWords()
    {
        List<string> words = [];

        var lines = File.ReadLines(@"../../../Files/wordlist.txt");
        Random rnd = new();
        while (words.Count < 3)
        {
            string word = lines.ElementAt(rnd.Next(lines.Count()));
            if (!AllUsedWords.Contains(word))
                words.Add(word);
        }

        return words;
    }

    public int GetGuessScore()
    {
        if (DoodleTime <= 0)
            return 0;
        double ratio = (double)_timeRemaining / DoodleTime;
        double speedBonus = 0.5 + ratio;
        return (int)Math.Round(MAX_POINTS * ratio * speedBonus);
    }

    public int GetDrawerScore()
    {
        // AI: i didnt know how to calculate the Doodlers points
        int requiredGuessers = Players.Count(p => p.PlayerData.GUID != CurrentTurnPlayerGuid);
        if (requiredGuessers <= 0 || DoodleTime <= 0)
            return 0;
        double guessedRatio = (double)AllGuessedPlayers.Count / requiredGuessers;
        double timeRatio = Math.Clamp((double)_timeRemaining / DoodleTime, 0, 1);
        return (int)Math.Round(MAX_POINTS * guessedRatio * timeRatio);
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

    public Dictionary<Guid, int> GetRoundPoints()
    {
        Dictionary<Guid, int> roundPoints = [];

        foreach (ServerPlayer player in Players)
        {
            int startScore = RoundStartScores.TryGetValue(player.PlayerData.GUID, out int score) ? score : player.PlayerData.Score;
            roundPoints[player.PlayerData.GUID] = player.PlayerData.Score - startScore;
        }

        return roundPoints;
    }

    private void InitializeRound()
    {
        AllGuessedPlayers.Clear();
        RoundStartScores = Players.ToDictionary(p => p.PlayerData.GUID, p => p.PlayerData.Score);
        _allGuessedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EndedByTime = false;
        _timeRemaining = DoodleTime;
        IsRoundStarted = true;
    }

    public async Task RunRoundAsync(CancellationToken cancellationToken = default)
    {
        InitializeRound();

        Dictionary<int, char> letterIndexes = [];
        for (int i = 0; i < CurrentWord.Length; i++)
        {
            if (CurrentWord[i] != ' ')
                letterIndexes.Add(i, CurrentWord[i]);
        }

        Random rnd = new();
        int revealCount = Math.Min(4, letterIndexes.Count / 2);
        int revealDuration = Math.Max(1, DoodleTime / 2);
        int requiredGuessers = Players.Count(p => p.PlayerData.GUID != CurrentTurnPlayerGuid);
        while (_timeRemaining > 0 && AllGuessedPlayers.Count < requiredGuessers)
        {
            int wait = Math.Min(revealDuration, _timeRemaining);

            bool endedEarly = false;
            for (int i = 0; i < wait; i++)
            {
                Task delayTask = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                Task completedTask = await Task.WhenAny(delayTask, _allGuessedTcs.Task);

                if (completedTask == _allGuessedTcs.Task)
                {
                    endedEarly = true;
                    break;
                }

                _timeRemaining--;
                if (_timeRemaining <= 0)
                    break;
            }

            if (endedEarly)
                break;

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

        EndedByTime = _timeRemaining <= 0 && AllGuessedPlayers.Count < requiredGuessers;
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
