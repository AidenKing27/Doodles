using GameLibrary.Models;
using System.Collections.Concurrent;
using System.IO;

namespace DoodlesServer.Models;

public class Room(ServerPlayer host, string code, int playerCount, int doodleTime, int rounds, List<ServerPlayer> players)
{
    public delegate Task RoomLetterHandler(Room room, char letter);
    public event RoomLetterHandler? RevealLetterEvent;

    public delegate Task RoomRoundHandler(Room room);
    public event RoomRoundHandler? EndRoundEvent;

    public ServerPlayer Host { get; set; } = host;
    public string Code { get; set; } = code;
    public int MaxPlayerCount { get; set; } = playerCount;
    public int DoodleTime { get; set; } = doodleTime;
    public int Rounds { get; set; } = rounds;
    public bool IsStarted { get; set; }
    public List<ServerPlayer> Players { get; set; } = players;
    public Queue<ServerPlayer> PlayerQueue { get; set; } = [];
    public ConcurrentQueue<DoodleInfo> DoodleQueue { get; set; } = [];
    public string CurrentWord { get; set; } = string.Empty;
    public List<string> AllUsedWords { get; set; } = [];

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

    private char GetRandomRevealedLetter()
    {
        Random rnd = new();
        int index = rnd.Next(CurrentWord.Length);
        return CurrentWord[index];
    }

    public async Task RunRoundAsync(CancellationToken cancellationToken = default)
    {
        int remaining = DoodleTime;
        int revealDuration = Math.Max(1, DoodleTime / 2);
        int revealCount = CurrentWord.Length / 2;

        while (remaining > 0)
        {
            int wait = Math.Min(revealDuration, remaining);
            await Task.Delay(TimeSpan.FromSeconds(wait), cancellationToken);
            remaining -= wait;
            revealDuration = Math.Max(1, revealDuration / 2);

            if (remaining > 0 && revealCount > 0)
            {
                char letter = GetRandomRevealedLetter();
                await InvokeRevealLetterEventAsync(letter);
                revealCount--;
            }
        }

        await InvokeEndRoundEventAsync();
    }

    // AI: async event
    private async Task InvokeRevealLetterEventAsync(char letter)
    {
        if (RevealLetterEvent is not { } handlers) return;

        foreach (RoomLetterHandler handler in handlers.GetInvocationList())
        {
            await handler(this, letter);
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
