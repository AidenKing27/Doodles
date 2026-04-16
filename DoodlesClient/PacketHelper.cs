using GameLibrary.Core;
using GameLibrary.Enums;
using GameLibrary.Models;
using System.Windows;
using System.Windows.Media;

namespace DoodlesClient;

public class PacketHelper
{
    public static void SendPacketToServer(Client client, PacketType type, object content)
    {
        _ = client.SendPacket(type, content);
    }

    public static DoodleInfo CreateDoodleInfo(DoodleType type, int? thickness, Color? colour, Point? p = null)
    {
        return new()
        {
            DoodleType = type,
            Point = p,
            UserThickness = thickness,
            UserColour = colour
        };
    }
}
