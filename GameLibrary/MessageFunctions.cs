using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows.Interop;

namespace GameLibrary;

public static class MessageFunctions
{
    private const string DELIM = "<!EOM!>";

    public static async Task SendPacket(TcpClient client, MessageType type, object content)
    {
        Packet packet = new()
        {
            ContentType = type,
            Content = content
        };

        NetworkStream ns = client.GetStream();
        string json = JsonSerializer.Serialize(packet) + DELIM;
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        await ns.WriteAsync(buffer, 0, buffer.Length);
    }
}
