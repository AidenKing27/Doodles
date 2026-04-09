using GameLibrary.Enums;
using GameLibrary.Models;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows.Interop;

namespace GameLibrary.Core;

public static class MessageFunctions
{
    private const string DELIM = "<!EOM!>";

    public static Packet CreatePacket(ContentType type, object content)
    {
        return new()
        {
            ContentType = type,
            Content = JsonSerializer.Serialize(content)
        };
    }

    public static async Task SendPacket(TcpClient client, Packet packet)
    {
        NetworkStream ns = client.GetStream();
        string json = JsonSerializer.Serialize(packet) + DELIM;
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        await ns.WriteAsync(buffer, 0, buffer.Length);
    }
}
