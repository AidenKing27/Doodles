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
}
