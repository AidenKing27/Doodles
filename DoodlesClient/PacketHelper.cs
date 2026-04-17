using GameLibrary.Core;
using GameLibrary.Enums;
using GameLibrary.Models;
using System.Windows;
using System.Windows.Media;

namespace DoodlesClient;

public class PacketHelper
{
    public static async Task SendPacketToServer(Client client, PacketType type, object content)
    {
        await client.SendPacket(type, content);
    }

    public static DoodleInfo CreateDoodleInfo(DoodleType type, int? thickness, Color? colour, Point? p = null, bool? isErasing = null)
    {
        return new()
        {
            DoodleType = type,
            Point = p,
            UserThickness = thickness,
            UserColour = colour
        };
    }

    public static DoodleInfo CreateDoodleInfoErasing(DoodleType type, bool isErasing)
    {
        return new()
        {
            DoodleType = type,
            IsErasing = isErasing
        };
    }

    public static DoodleInfo CreateDoodleInfoThickness(DoodleType type, int thickness)
    {
        return new()
        {
            DoodleType = type,
            UserThickness = thickness
        };
    }
}
