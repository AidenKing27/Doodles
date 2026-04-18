using GameLibrary.Enums;

namespace GameLibrary.Models;

public class Packet(PacketType type, string? content = null)
{
    public PacketType Type { get; set; } = type;
    public string? Content { get; set; } = content;
}
