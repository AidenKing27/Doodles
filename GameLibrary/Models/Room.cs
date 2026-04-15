using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Models;

public class Room
{
    public string Code { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
    public int Drawtime { get; set; }
    public int Rounds { get; set; }
    public List<ServerPlayer> Players { get; set; } = [];

    public Room()
    {
        
    }

    public Room(string code, int playerCount, int drawtime, int rounds, List<ServerPlayer> players)
    {
        Code = code;
        PlayerCount = playerCount;
        Drawtime = drawtime;
        Rounds = rounds;
        Players = players;
    }

    public Room(List<ServerPlayer> players)
    {
        Players = players;
    }
}
