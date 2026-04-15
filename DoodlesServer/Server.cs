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

    private TcpListener _listener;
    private List<TcpClient> _clients = [];
    private List<ServerPlayer> _players = [];
    private Dictionary<string, Room> _rooms = [];
    private bool _isRunning;

    public delegate void ServerMessageHandler(string message);
    public event ServerMessageHandler? ServerMessageEvent;
    public event ServerMessageHandler? ConnectMessageEvent;
    public event ServerMessageHandler? DisconnectMessageEvent;

    public Server(int port)
    {
        _listener = new(IPAddress.Any, port);
    }

    public async Task Start()
    {
        _listener.Start();
        _isRunning = true;
        ServerMessageEvent?.Invoke($"Server Started {_listener.LocalEndpoint}");

        while (_isRunning)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _clients.Add(client);

            _ = Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        ServerPlayer player = new(client, new());
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
            await HandleDisconnect(player);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

            await HandleDisconnect(player);
            //await HandleConnectionLoss(player);
        }
    }

    private async Task ProcessPacket(Packet packet, ServerPlayer player)
    {
        switch (packet.ContentType)
        {
            case ContentType.Message:
                await HandleMessage(packet, player);
                break;

            case ContentType.CreateRoom:
                await HandleCreateRoom(packet, player);
                break;

            case ContentType.PlayerData:
                await HandlePlayerData(packet, player);
                break;

            case ContentType.Connect:
                await HandleConnect(packet, player);
                break;

            case ContentType.Disconnect:
                await HandleDisconnect(player);
                break;

            case ContentType.RoomCodes:
                await HandleRoomCodes(player);
                break;

            case ContentType.Doodle:
                await HandleDoodle(packet);
                break;

            default:
                break;
        }
    }

    private async Task HandleMessage(Packet packet, ServerPlayer player)
    {
        foreach (ServerPlayer p in _rooms[player.PlayerData.RoomCode].Players)
            await BroadcastMessage(p.Client, ContentType.Message, packet);

        ServerMessageEvent?.Invoke($"[{player.PlayerData.RoomCode}] {player.PlayerData.Username}: {JsonSerializer.Deserialize<string>(packet.Content!)!}");
    }

    private async Task HandleCreateRoom(Packet packet, ServerPlayer player)
    {

    }

    private async Task HandlePlayerData(Packet packet, ServerPlayer player)
    {
        player.PlayerData = JsonSerializer.Deserialize<PlayerData>(packet.Content!)!;

        string roomCode = player.PlayerData.RoomCode;
        if (_rooms.TryGetValue(roomCode, out Room? value))
        {
            value.Players.Add(player);
            foreach (ServerPlayer p in value.Players)
                await BroadcastMessage(p.Client, ContentType.Connect, packet);
        }
        //else
        //{
        //    Room newRoom = new([player]);
        //    _rooms.Add(roomCode, newRoom);
        //    foreach (ServerPlayer p in newRoom.Players)
        //        await BroadcastMessage(p.Client, ContentType.Connect, packet);
        //}

        ConnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} joined Room: {roomCode} ({player.Client.Client.RemoteEndPoint})");
    }

    private async Task HandleConnect(Packet packet, ServerPlayer player)
    {
        _players.Add(player);

        ConnectMessageEvent?.Invoke($"[SERVER]: User connected to the server ({player.Client.Client.RemoteEndPoint})");
    }

    private async Task HandleDisconnect(ServerPlayer player)
    {
        if (player.PlayerData.Username == "") return;

        string roomCode = player.PlayerData.RoomCode;
        if (_rooms.TryGetValue(roomCode, out Room? value))
        {
            foreach (ServerPlayer p in value.Players)
                await BroadcastMessage(p.Client, ContentType.Disconnect, $"{player.PlayerData.Username}");
        }

        DisconnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} disconnected from the server ({player.Client.Client.RemoteEndPoint})");
    }

    //private async Task HandleConnectionLoss(ServerPlayer player)
    //{
    //    if (_rooms.TryGetValue(roomCode, out Room? value))
    //    {
    //        foreach (ServerPlayer p in value.Players)
    //            await BroadcastMessage(p.Client, ContentType.Disconnect, $"{player.PlayerData.Username}");
    //    }

    //    DisconnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} lost connection to the server ({player.Client.Client.RemoteEndPoint})");
    //}

    private async Task HandleRoomCodes(ServerPlayer player)
    {
        await BroadcastMessage(player.Client, ContentType.RoomCodes, new List<string>(_rooms.Keys));
    }

    private async Task HandleDoodle(Packet packet)
    {

    }

    public async Task BroadcastMessage(TcpClient client, ContentType type, object content)
    {
        //second method for testing
        if (content is Packet packet)
            await MessageFunctions.SendPacket(client, packet);
        else
            await MessageFunctions.SendPacket(client, MessageFunctions.CreatePacket(type, content));
    }

    //public async Task BroadcastMessage(ContentType type, object content)
    //{
    //    List<Task> sendTasks = clients.Select(client =>
    //        MessageFunctions.SendPacket(client, type, content)).ToList();

    //    await Task.WhenAll(sendTasks);

    //    //Gotchas in this implementation
    //    //  •	clients is a shared mutable list; if clients are added/ removed while broadcasting, this can throw or behave unpredictably.
    //    //  •	If any send task fails, Task.WhenAll faults(you’ll need try/catch if you want partial success behavior).
    //    //  •	The method sends to all entries in clients, even potentially disconnected ones unless cleanup is handled elsewhere.
    //}
}
