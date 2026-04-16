using GameLibrary.Enums;

namespace GameLibrary.Models;

public class Packet
{
    public PacketType Type { get; set; }
    public string? Content { get; set; }
}
