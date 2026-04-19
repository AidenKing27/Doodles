using GameLibrary.Enums;

namespace GameLibrary.Models;

public class RoomInfo()
{
    public RoomActionType RoomActionType { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public bool? IsStarted { get; set; }
    public string? ChosenWord { get; set; } = string.Empty;
    public int? WordLength { get; set; }
    public char? RevealedLetter { get; set; }
    public int? RevealedLetterIndex { get; set; }
    public List<string>? WordList { get; set; }
    public bool? RoundOver { get; set; }
    public PlayerData? CurrentTurnPlayer { get; set; }

    public static RoomInfo SendIsStarted(RoomActionType type, string roomCode, bool isStarted)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            IsStarted = isStarted
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

    public static RoomInfo SendWordLength(RoomActionType type, string roomCode, int wordLength)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            WordLength = wordLength
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

    public static RoomInfo SendRoundOver(RoomActionType type, string roomCode, bool roundOver)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            RoundOver = roundOver
        };
    }
}
