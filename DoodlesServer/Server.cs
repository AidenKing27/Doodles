using DoodlesServer.Models;
using GameLibrary.Core;
using GameLibrary.Enums;
using GameLibrary.Models;
using System.Collections.Concurrent;
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
        switch (packet.Type)
        {
            case PacketType.Message:
                await HandleMessage(packet, player);
                break;

            case PacketType.CreateRoom:
                await HandleCreateRoom(packet, player);
                break;

            case PacketType.JoinRoom:
                await HandleJoinRoom(packet, player);
                break;

            case PacketType.PlayerData:
                await HandlePlayerData(packet, player);
                break;

            case PacketType.ServerConnect:
                await HandleConnect(packet, player);
                break;

            case PacketType.Disconnect:
                await HandleDisconnect(player);
                break;

            case PacketType.RoomCodes:
                await HandleRoomCodes(player);
                break;

            case PacketType.Doodle:
                await HandleDoodle(packet, player);
                break;

            case PacketType.RoomInfo:
                await HandleRoomInfo(packet, player);
                break;

            default:
                break;
        }
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
        if (_rooms.TryGetValue(roomCode, out Room? room))
        {
            foreach (ServerPlayer p in room.Players)
                await BroadcastMessage(
                    p.Client, 
                    PacketType.Disconnect, 
                    $"{player.PlayerData.Username}");
        }

        DisconnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} disconnected from the server ({player.Client.Client.RemoteEndPoint})");
    }

    //private async Task HandleConnectionLoss(ServerPlayer player)
    //{
    //    if (_rooms.TryGetValue(roomCode, out Room? value))
    //    {
    //        foreach (ServerPlayer p in value.Players)
    //            await BroadcastMessage(p.Client, PacketType.Disconnect, $"{player.PlayerData.Username}");
    //    }

    //    DisconnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} lost connection to the server ({player.Client.Client.RemoteEndPoint})");
    //}

    private async Task HandleMessage(Packet packet, ServerPlayer player)
    {
        Message message = JsonSerializer.Deserialize<Message>(packet.Content!)!;

        foreach (ServerPlayer p in _rooms[player.PlayerData.RoomCode].Players)
            await BroadcastMessage(
                p.Client, 
                PacketType.Message, 
                message);

        ServerMessageEvent?.Invoke($"[{player.PlayerData.RoomCode}] {message.Sender}: {message.Content}");
    }

    private async Task HandlePlayerData(Packet packet, ServerPlayer player)
    {
        player.PlayerData = JsonSerializer.Deserialize<PlayerData>(packet.Content!)!;
    }

    private async Task HandleRoomCodes(ServerPlayer player)
    {
        await BroadcastMessage(
            player.Client, 
            PacketType.RoomCodes, 
            new List<string>(_rooms.Keys));
    }

    private async Task HandleCreateRoom(Packet packet, ServerPlayer player)
    {
        RoomDto roomDto = JsonSerializer.Deserialize<RoomDto>(packet.Content!)!;
        Room newRoom = new(player, roomDto.Code, roomDto.MaxPlayerCount, roomDto.DoodleTime, roomDto.Rounds, [player]);
        newRoom.RevealLetterEvent += Room_RevealLetterEvent;
        newRoom.EndRoundEvent += Room_EndRoundEvent;
        _rooms.Add(roomDto.Code, newRoom);
        await BroadcastMessage(
            player.Client, 
            PacketType.PlayerData, 
            player.PlayerData);

        ConnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} has created and joined Room: {roomDto.Code} ({player.Client.Client.RemoteEndPoint})");
    }

    private async Task HandleJoinRoom(Packet packet, ServerPlayer player)
    {
        string roomCode = player.PlayerData.RoomCode;
        if (_rooms.TryGetValue(roomCode, out Room? room))
        {
            if (room.Players.Count >= room.MaxPlayerCount) return;

            room.Players.Add(player);
            room.PlayerQueue.Enqueue(player);
            foreach (ServerPlayer p in room.Players)
            {
                //tell every player of the new client
                await BroadcastMessage(
                    p.Client, 
                    PacketType.PlayerData, 
                    player.PlayerData);

                //tell the new client of every player
                await BroadcastMessage(
                    player.Client, 
                    PacketType.PlayerData, 
                    p.PlayerData);
            }

            ConnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} joined Room: {roomCode} ({player.Client.Client.RemoteEndPoint})");
        }
    }

    private async Task HandleRoomInfo(Packet packet, ServerPlayer player)
    {
        if (_rooms.TryGetValue(player.PlayerData.RoomCode, out Room? room))
        {
            RoomInfo info = JsonSerializer.Deserialize<RoomInfo>(packet.Content!)!;
            switch (info.RoomActionType)
            {
                case RoomActionType.Start:
                    room.IsStarted = true;
                    ServerPlayer selectedPlayer = room.PlayerQueue.Dequeue();
                    room.PlayerQueue.Enqueue(selectedPlayer);
                    foreach (ServerPlayer p in room.Players)
                    {
                        await BroadcastMessage(
                            p.Client, 
                            PacketType.RoomInfo, 
                            packet);

                        await BroadcastMessage(
                            p.Client, 
                            PacketType.RoomInfo, 
                            RoomInfo.SendCurrentTurnPlayer(RoomActionType.SelectPlayer, room.Code, selectedPlayer.PlayerData));
                    }

                    await BroadcastMessage(
                        selectedPlayer.Client, 
                        PacketType.RoomInfo, 
                        RoomInfo.SendWordList(RoomActionType.WordList, room.Code, room.GetThreeRandomWords()));

                    break;

                case RoomActionType.ChosenWord:
                    room.CurrentWord = info.ChosenWord!;

                    foreach (ServerPlayer p in room.Players)
                    {
                        await BroadcastMessage(
                            p.Client, 
                            PacketType.RoomInfo, 
                            RoomInfo.SendWordLength(RoomActionType.RoundStart, room.Code, room.CurrentWord.Length));
                    }

                    _ = room.RunRoundAsync();
                    break;

                default:
                    break;
            }
        }
    }

    //callbacks from room.RunRoundAsync();
    private async Task Room_RevealLetterEvent(Room room, char letter)
    {
        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client, 
                PacketType.RoomInfo, 
                RoomInfo.SendRandomLetterReveal(RoomActionType.RevealedLetter, room.Code, letter, room.CurrentWord.IndexOf(letter)));
        }
    }

    //callbacks from room.RunRoundAsync();
    private async Task Room_EndRoundEvent(Room room)
    {
        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client, 
                PacketType.RoomInfo, 
                RoomInfo.SendRoundOver(RoomActionType.RoundEnd, room.Code, true));
        }
    }

    private async Task HandleDoodle(Packet packet, ServerPlayer player)
    {
        if (_rooms.TryGetValue(player.PlayerData.RoomCode, out Room? room))
        {
            room.DoodleQueue.Enqueue(JsonSerializer.Deserialize<DoodleInfo>(packet.Content!)!);
            if (room.DoodleQueue.TryDequeue(out DoodleInfo? doodleInfo))
                foreach (ServerPlayer p in room.Players)
                    if (p.PlayerData.Username != player.PlayerData.Username)
                        await BroadcastMessage(p.Client, PacketType.Doodle, doodleInfo!);
        }
    }

    public async Task BroadcastMessage(TcpClient client, PacketType type, object content)
    {
        //second method for testing
        if (content is Packet packet)
            await MessageFunctions.SendPacket(client, packet);
        else
            await MessageFunctions.SendPacket(client, MessageFunctions.CreatePacket(type, content));
    }

    //public async Task BroadcastMessage(PacketType type, object content)
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
