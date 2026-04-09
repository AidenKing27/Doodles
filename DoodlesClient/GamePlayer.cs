using GameLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoodlesClient;

public class GamePlayer
{
    public PlayerData PlayerData { get; set; }
    public bool IsPlayer { get; set; }

    public GamePlayer(PlayerData data, bool isPlayer)
    {
        PlayerData = data;
        IsPlayer = isPlayer;
    }
}
