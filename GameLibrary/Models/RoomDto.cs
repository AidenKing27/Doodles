using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Models;

public class RoomDto(string code, int playerCount, int doodleTime, int rounds)
{
    public string Code { get; set; } = code;
    public int PlayerCount { get; set; } = playerCount;
    public int DoodleTime { get; set; } = doodleTime;
    public int Rounds { get; set; } = rounds;
}
