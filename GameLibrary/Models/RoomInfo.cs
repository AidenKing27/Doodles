using GameLibrary.Enums;

namespace GameLibrary.Models;

public class RoomInfo()
{
    public RoomActionType RoomActionType { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public bool? IsGameStarted { get; set; }
    public bool? IsRoundActive { get; set; }
    public List<string>? WordList { get; set; }
    public string? ChosenWord { get; set; } = string.Empty;
    public char? RevealedLetter { get; set; }
    public int? RevealedLetterIndex { get; set; }
    public PlayerData? CurrentTurnPlayer { get; set; }
    public PlayerData? UpdatePlayer { get; set; }
    public PlayerData? CorrectGuesser { get; set; }
    public string? PlayerGuess { get; set; }
    public Dictionary<Guid, PlayerRankPair>? PlayerRankings { get; set; }

    public static RoomInfo SendIsStarted(RoomActionType type, string roomCode, bool isStarted)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            IsGameStarted = isStarted
        };
    }

    public static RoomInfo SendCurrentTurnPlayer(RoomActionType type, string roomCode, PlayerData currentTurnPlayer)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            CurrentTurnPlayer = currentTurnPlayer
        };
    }

    public static RoomInfo SendWordList(RoomActionType type, string roomCode, List<string> words)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            WordList = words
        };
    }

    public static RoomInfo SendChosenWord(RoomActionType type, string roomCode, string chosenWord)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            ChosenWord = chosenWord
        };
    }

    public static RoomInfo SendRoundStart(RoomActionType type, string roomCode, string chosenWord, bool isRoundStarted)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            ChosenWord = chosenWord,
            IsRoundActive = isRoundStarted
        };
    }

    public static RoomInfo SendRandomLetterReveal(RoomActionType type, string roomCode, char letter, int letterIndex)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            RevealedLetter = letter,
            RevealedLetterIndex = letterIndex
        };
    }

    public static RoomInfo SendRoundOver(RoomActionType type, string roomCode, bool isRoundActive)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            IsRoundActive = isRoundActive
        };
    }

    public static RoomInfo SendGuess(RoomActionType type, string roomCode, string guess)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            PlayerGuess = guess
        };
    }

    public static RoomInfo SendScoreUpdate(RoomActionType type, string roomCode, PlayerData correctGuesser, Dictionary<Guid, PlayerRankPair> playerRankings)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            CorrectGuesser = correctGuesser,
            PlayerRankings = playerRankings
        };
    }
}
