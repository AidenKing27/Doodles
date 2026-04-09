using GameLibrary.Enums;

namespace GameLibrary.Models;

public class Packet
{
    public ContentType ContentType { get; set; }
    public string? Content { get; set; }
}
