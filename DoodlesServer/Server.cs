using GameLibrary.Core;
using GameLibrary.Enums;
using GameLibrary.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DoodlesServer;

public class Server
{
    private const string DELIM = "<!EOM!>";

    private TcpListener listener;
    private List<TcpClient> clients = new();
    private List<Player> allPlayers = new();
    private Dictionary<string, List<Player>> gameRooms = new();
    private bool isRunning;

    public delegate void ServerMessageHandler(string message);
    public event ServerMessageHandler? ServerMessageEvent;
    public event ServerMessageHandler? ConnectMessageEvent;
    public event ServerMessageHandler? DisconnectMessageEvent;

    public Server(int port)
    {
        listener = new(IPAddress.Any, port);
    }

    public async Task Start()
    {
        listener.Start();
        isRunning = true;
        ServerMessageEvent?.Invoke($"Server Started {listener.LocalEndpoint}");

        while (isRunning)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            clients.Add(client);

            _ = Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        Player player = new(client, new PlayerData("", ""));
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
                    await ProcessPacket(JsonSerializer.Deserialize<Packet>(allMessages[0])!, player);
                    allMessages.RemoveAt(0);
                }

                spool = "";
                foreach (string m in allMessages)
                    spool += m;
            }
            await HandleDisconnect(player, "disconnected from");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

            await HandleDisconnect(player, "lost connection to");
        }
    }

    private async Task ProcessPacket(Packet packet, Player player)
    {
        switch (packet.ContentType)
        {
            case ContentType.Message:
                await HandleMessage(packet, player);
                break;

            case ContentType.Connect:
                await HandleConnect(packet, player);
                break;

            case ContentType.Disconnect:
                await HandleDisconnect(player, "disconnected from");
                break;

            case ContentType.Doodle:
                await HandleDoodle();
                break;

            default:
                break;
        }
    }

    private async Task HandleMessage(Packet packet, Player player)
    {
        foreach (Player p in gameRooms[player.PlayerData.RoomCode])
            await BroadcastMessage(p.Client, ContentType.Message, packet);

        ServerMessageEvent?.Invoke($"[{player.PlayerData.RoomCode}] {player.PlayerData.Username}: {JsonSerializer.Deserialize<string>(packet.Content!)!}");
    }

    private async Task HandleConnect(Packet packet, Player player)
    {
        player.PlayerData = JsonSerializer.Deserialize<PlayerData>(packet.Content!)!;
        allPlayers.Add(player);

        string roomCode = player.PlayerData.RoomCode;
        if (gameRooms.TryGetValue(roomCode, out List<Player>? value))
        {
            value.Add(player);
            foreach (Player p in value)
                await BroadcastMessage(p.Client, ContentType.Connect, packet);
        }
        else
        {
            List<Player> newRoomPlayers = [player];
            gameRooms.Add(roomCode, newRoomPlayers);
            foreach (Player p in newRoomPlayers)
                await BroadcastMessage(p.Client, ContentType.Connect, packet);
        }

        ConnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} connected to the server in Room: {roomCode} ({player.Client.Client.RemoteEndPoint})");
    }

    private async Task HandleDisconnect(Player player, string message)
    {
        if (player.PlayerData.Username == "") return;

        foreach (var client in clients)
            await BroadcastMessage(client, ContentType.Disconnect, $"{player.PlayerData.Username}");

        DisconnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} {message} the server ({player.Client.Client.RemoteEndPoint})");
    }

    private async Task HandleDoodle()
    {

    }

    public async Task BroadcastMessage(ContentType type, object content)
    {
        //List<Task> sendTasks = clients.Select(client =>
        //    MessageFunctions.SendPacket(client, type, content)).ToList();

        //await Task.WhenAll(sendTasks);

        ////Gotchas in this implementation
        ////  •	clients is a shared mutable list; if clients are added/ removed while broadcasting, this can throw or behave unpredictably.
        ////  •	If any send task fails, Task.WhenAll faults(you’ll need try/catch if you want partial success behavior).
        ////  •	The method sends to all entries in clients, even potentially disconnected ones unless cleanup is handled elsewhere.
    }

    public async Task BroadcastMessage(TcpClient client, ContentType type, object content)
    {
        //second method for testing
        if (content is Packet packet)
            await MessageFunctions.SendPacket(client, packet);
        else
            await MessageFunctions.SendPacket(client, MessageFunctions.CreatePacket(type, content));
    }
}
