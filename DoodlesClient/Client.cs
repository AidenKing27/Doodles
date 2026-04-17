using GameLibrary.Core;
using GameLibrary.Enums;
using GameLibrary.Models;
using System.Diagnostics;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DoodlesClient;

public class Client
{
    private const string DELIM = "<!EOM!>";
    private string Username = "TESTTESTTEST";

    public ClientPlayer CurrentClientPlayer { get; set; }
    private TcpClient client;
    private static List<ClientPlayer> connectedPlayers = new();
    //private static Queue<GamePlayer> playerQueue;

    public delegate void ClientRoomHandler(List<string> roomCodes);
    public event ClientRoomHandler? RoomListEvent;

    public delegate void ClientMessageHandler(string message);
    public event ClientMessageHandler? ClientMessageEvent;

    public delegate void ClientPlayerHandler(string message, List<ClientPlayer> connectedPlayers);
    public event ClientPlayerHandler? ConnectMessageEvent;
    public event ClientPlayerHandler? DisconnectMessageEvent;

    public delegate void ClientDoodleHandler(DoodleInfo doodleInfo);
    public event ClientDoodleHandler? DownEvent;
    public event ClientDoodleHandler? MoveEvent;
    public event ClientDoodleHandler? UpEvent;
    public event ClientDoodleHandler? UndoEvent;
    public event ClientDoodleHandler? ClearEvent;

    public bool IsConnected => client?.Connected ?? false;

    public Client(string host, int port)
    {
        client = new(host, port);
        Task.Run(() => Receive());
    }

    public async Task Receive()
    {
        try
        {
            NetworkStream ns = client.GetStream();
            byte[] buffer = new byte[4096];
            int bytesRead;
            string spool = "";

            while ((bytesRead = await ns.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                spool += Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (!spool.Contains(DELIM)) continue;

                int count = Regex.Matches(spool, DELIM).Count;
                List<string> allMessages = spool.Split(DELIM).ToList();
                if (allMessages[^1] == "") allMessages.RemoveAt(allMessages.Count - 1);

                for (int i = 0; i < count; i++)
                {
                    await ProcessPacket(JsonSerializer.Deserialize<Packet>(allMessages[0])!);
                    allMessages.RemoveAt(0);
                }

                spool = "";
                foreach (string m in allMessages)
                    spool += m;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

            throw;
        }
    }

    private async Task ProcessPacket(Packet packet)
    {
        switch (packet.Type)
        {
            case PacketType.Message:
                await HandleMessage(packet);
                break;

            case PacketType.PlayerData:
                await HandlePlayerData(packet);
                break;

            case PacketType.Connect:
                await HandleConnect(packet);
                break;

            case PacketType.Disconnect:
                await HandleDisconnect(packet);
                break;

            case PacketType.RoomCodes:
                await HandleRoomCodes(packet);
                break;

            case PacketType.Doodle:
                await HandleDoodle(packet);
                break;

            default:
                break;
        }
    }

    private async Task HandleMessage(Packet packet)
    {
        Message message = JsonSerializer.Deserialize<Message>(packet.Content!)!;

        ClientMessageEvent?.Invoke($"{message.Sender}: {message.Content}");
    }

    private async Task HandlePlayerData(Packet packet)
    {

    }

    private async Task HandleConnect(Packet packet)
    {
        PlayerData newPlayerData = JsonSerializer.Deserialize<PlayerData>(packet.Content!)!;
        if (!connectedPlayers.Any(p => p.PlayerData.Username == newPlayerData.Username))
        {
            bool isCurrentClientPlayer = newPlayerData.Username == Username;
            ClientPlayer gamePlayer = new(newPlayerData, isCurrentClientPlayer);
            connectedPlayers.Add(gamePlayer);
            ConnectMessageEvent?.Invoke($"{gamePlayer.PlayerData.Username} joined the room!", [.. connectedPlayers]);
            if (isCurrentClientPlayer)
                CurrentClientPlayer = gamePlayer;
        }
    }

    private async Task HandleDisconnect(Packet packet)
    {
        ClientPlayer? disconnectingUser = connectedPlayers.FirstOrDefault(p => p.PlayerData.Username == JsonSerializer.Deserialize<string>(packet.Content!));
        if (disconnectingUser != null)
        {
            connectedPlayers.Remove(disconnectingUser);
            DisconnectMessageEvent?.Invoke($"{disconnectingUser.PlayerData.Username} left the room!", [.. connectedPlayers]);
        }
    }

    private async Task HandleRoomCodes(Packet packet)
    {
        RoomListEvent?.Invoke(JsonSerializer.Deserialize<List<string>>(packet.Content!)!);
    }

    private async Task HandleDoodle(Packet packet)
    {
        DoodleInfo doodleInfo = JsonSerializer.Deserialize<DoodleInfo>(packet.Content!)!;
        switch (doodleInfo.DoodleType)
        {
            case DoodleType.Down:
                DownEvent?.Invoke(doodleInfo);
                break;

            case DoodleType.Move:
                MoveEvent?.Invoke(doodleInfo);
                break;

            case DoodleType.Up:
                UpEvent?.Invoke(doodleInfo);
                break;

            case DoodleType.Undo:
                UndoEvent?.Invoke(doodleInfo);
                break;

            case DoodleType.Clear:
                ClearEvent?.Invoke(doodleInfo);
                break;
        }
    }

    public async Task SendPacket(PacketType type, object content)
    {
        if (type == PacketType.PlayerData)
            Username = ((PlayerData)content).Username;

        await MessageFunctions.SendPacket(client, MessageFunctions.CreatePacket(type, content));
    }
}
