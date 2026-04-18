using GameLibrary.Enums;

namespace GameLibrary.Models;

public class RoomInfo()
{
    public RoomActionType RoomActionType { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public bool? IsStarted { get; set; }
    public List<string>? Words { get; set; }
    public bool? RoundOver { get; set; }
    public PlayerData CurrentTurnPlayer { get; set; }

    public static RoomInfo CreateRoomInfoIsStarted(RoomActionType type, string roomCode, bool isStarted)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            IsStarted = isStarted
        };
    }

    public static RoomInfo CreateRoomInfoCurrentTurnPlayer(RoomActionType type, string roomCode, PlayerData currentTurnPlayer)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            CurrentTurnPlayer = currentTurnPlayer
        };
    }

    public static RoomInfo CreateRoomInfoWordList(RoomActionType type, string roomCode, List<string> words)
    {
        return new()
        {
            RoomActionType = type,
            RoomCode = roomCode,
            Words = words
        };
    }
}
