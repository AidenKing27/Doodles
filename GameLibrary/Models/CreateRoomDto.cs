using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Models;

public class CreateRoomDto(string code, int maxPlayerCount, int doodleTime, int rounds)
{
    public string Code { get; set; } = code;
    public int MaxPlayerCount { get; set; } = maxPlayerCount;
    public int DoodleTime { get; set; } = doodleTime;
    public int Rounds { get; set; } = rounds;
}
