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
        }
    }

    private async Task ProcessPacket(Packet packet, ServerPlayer player)
    {
        switch (packet.Type)
        {

            case PacketType.ServerConnect:
                await HandleServerConnect(packet, player);
                break;

            case PacketType.Disconnect:
                await HandleDisconnect(player);
                break;

            case PacketType.Message:
                await HandleMessage(packet, player);
                break;

            case PacketType.PlayerData:
                await HandlePlayerData(packet, player);
                break;

            case PacketType.RoomCodes:
                await HandleRoomCodes(player);
                break;

            case PacketType.CreateRoom:
                await HandleCreateRoom(packet, player);
                break;

            case PacketType.JoinRoom:
                await HandleJoinRoom(packet, player);
                break;

            case PacketType.RoomInfo:
                await HandleRoomInfo(packet, player);
                break;

            case PacketType.Doodle:
                await HandleDoodle(packet, player);
                break;

            default:
                break;
        }
    }

    private async Task HandleServerConnect(Packet packet, ServerPlayer player)
    {
        _players.Add(player);

        ConnectMessageEvent?.Invoke($"[SERVER]: User connected to the server ({player.Client.Client.RemoteEndPoint})");
    }

    private async Task HandleDisconnect(ServerPlayer player)
    {
        if (player.PlayerData.GUID == Guid.Empty) return;

        string roomCode = player.PlayerData.RoomCode;
        if (_rooms.TryGetValue(roomCode, out Room? room))
        {
            foreach (ServerPlayer p in room.Players)
                await BroadcastMessage(
                    p.Client,
                    PacketType.Disconnect,
                    player.PlayerData);
        }

        DisconnectMessageEvent?.Invoke($"[SERVER]: {player.PlayerData.Username} disconnected from the server ({player.Client.Client.RemoteEndPoint})");
    }

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
        try
        {
            CreateRoomDto roomDto = JsonSerializer.Deserialize<CreateRoomDto>(packet.Content!)!;
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
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }

    }

    private async Task HandleJoinRoom(Packet packet, ServerPlayer player)
    {
        string roomCode = player.PlayerData.RoomCode;
        if (_rooms.TryGetValue(roomCode, out Room? room))
        {
            if (room.Players.Count >= room.MaxPlayerCount) return;

            room.Players.Add(player);
            room.PlayerQueue.Enqueue(player);
            if (room.IsRoundStarted)
                room.RoundStartScores[player.PlayerData.GUID] = player.PlayerData.Score;

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

            if (room.IsGameStarted)
                await SendRoomStateSnapshotToPlayer(room, player);

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
                    ServerMessageEvent?.Invoke($"[{room.Code}] Start requested by {player.PlayerData.Username}");
                    await HandleRoomInfoStart(room, packet);
                    break;

                case RoomActionType.ChosenWord:
                    ServerMessageEvent?.Invoke($"[{room.Code}] Word ({info.ChosenWord}) selected by {player.PlayerData.Username}");
                    await HandleRoomInfoChosenWord(room, info);
                    break;

                case RoomActionType.Guess:
                    ServerMessageEvent?.Invoke($"[{room.Code}] Guess from {player.PlayerData.Username}: {info.GuessMessage?.Content}");
                    await HandleRoomInfoGuess(room, info, player);
                    break;

                default:
                    break;
            }
        }
    }

    private async Task HandleRoomInfoStart(Room room, Packet packet)
    {
        room.IsGameStarted = true;
        room.Phase = RoomPhase.ChoosingWord;
        room.RoundsPlayed = 0;

        if (!room.PlayerQueue.Any(p => p.PlayerData.GUID == room.Host.PlayerData.GUID))
            room.PlayerQueue.Enqueue(room.Host);

        ServerMessageEvent?.Invoke($"[{room.Code}] Game starting");
        await StartRoundAsync(room);

        // tell everyone the game has started
        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                packet);
        }
    }

    private async Task HandleRoomInfoChosenWord(Room room, RoomInfo info)
    {
        room.BeginDoodling(info.ChosenWord!);
        ServerMessageEvent?.Invoke($"[{room.Code}] Drawing phase started");

        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                RoomInfo.SendDrawingStarted(RoomActionType.DrawingStarted, room.Code, room.CurrentWord, true));
        }

        _ = room.RunRoundAsync();
    }

    private async Task HandleRoomInfoGuess(Room room, RoomInfo info, ServerPlayer player)
    {
        Message message = info.GuessMessage!;
        if (player.PlayerData.GUID == room.CurrentTurnPlayerGuid) return;
        if (!string.Equals(message.Content, room.CurrentWord, StringComparison.OrdinalIgnoreCase))
        {
            foreach (ServerPlayer p in _rooms[player.PlayerData.RoomCode].Players)
                await BroadcastMessage(
                    p.Client,
                    PacketType.Message,
                    message);
            return;
        }

        if (!room.AllGuessedPlayers.TryAdd(player.PlayerData.GUID, player.PlayerData)) return;

        player.PlayerData.Score += room.GetGuessScore();

        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                RoomInfo.SendScoreUpdate(RoomActionType.RoomUpdate, room.Code, player.PlayerData, room.GetRankOrder()));
        }

        ServerMessageEvent?.Invoke($"[{room.Code}] Score update for {player.PlayerData.Username}");

        ServerMessageEvent?.Invoke($"[{player.PlayerData.RoomCode}] {message.Sender}: {message.Content}");
        room.TryCompleteRoundIfAllGuessed();
    }

    //callbacks from room.RunRoundAsync();
    private async Task Room_RevealLetterEvent(Room room, char letter, int index)
    {
        room.RevealedLetters.Enqueue(new RevealedLetters(index, letter));
        ServerMessageEvent?.Invoke($"[{room.Code}] Revealed letter at index {index}: '{letter}'");

        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                RoomInfo.SendRandomLetterReveal(RoomActionType.RevealedLetter, room.Code, letter, index));
        }
    }

    //callbacks from room.RunRoundAsync();
    private async Task Room_EndRoundEvent(Room room)
    {
        room.IsRoundStarted = false;
        room.Phase = RoomPhase.RoundSummary;
        ServerMessageEvent?.Invoke($"[{room.Code}] Round ended ({(room.EndedByTime ? "time" : "guesses")})");

        ServerPlayer? drawer = room.Players.FirstOrDefault(p => p.PlayerData.GUID == room.CurrentTurnPlayerGuid);
        if (drawer is not null)
            drawer.PlayerData.Score += room.GetDrawerScore();

        Dictionary<Guid, PlayerRankPair> rankingSnapshot = room.GetRankOrder();

        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                RoomInfo.SendRankingSnapshot(RoomActionType.RoomUpdate, room.Code, rankingSnapshot));
        }

        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                RoomInfo.SendRoundOver(RoomActionType.RoundEnd, room.Code, false, room.CurrentWord, room.GetRoundPoints(), room.EndedByTime));
        }

        await Task.Delay(TimeSpan.FromSeconds(5));

        room.RoundsPlayed++;
        if (room.RoundsPlayed < room.MaxRounds * room.Players.Count)
            await StartRoundAsync(room);
        else
            await EndGameAsync(room);
    }

    private async Task StartRoundAsync(Room room)
    {
        ServerPlayer selectedPlayer = room.PlayerQueue.Dequeue();
        room.PlayerQueue.Enqueue(selectedPlayer);
        room.CurrentTurnPlayerGuid = selectedPlayer.PlayerData.GUID;
        room.BeginRound();

        ServerMessageEvent?.Invoke($"[{room.Code}] Current player: {selectedPlayer.PlayerData.Username}");

        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                RoomInfo.SendCurrentTurnPlayer(RoomActionType.SelectPlayer, room.Code, selectedPlayer.PlayerData));
        }

        ServerMessageEvent?.Invoke($"[{room.Code}] Round setup started");

        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                RoomInfo.SendRoundSetup(RoomActionType.RoundStart, room.Code));
        }

        ServerMessageEvent?.Invoke($"[{room.Code}] Sent word list to {selectedPlayer.PlayerData.Username}");

        await BroadcastMessage(
            selectedPlayer.Client,
            PacketType.RoomInfo,
            RoomInfo.SendWordList(RoomActionType.WordList, room.Code, room.GetThreeRandomWords()));
    }

    private async Task EndGameAsync(Room room)
    {
        room.IsGameStarted = false;
        room.IsRoundStarted = false;
        room.Phase = RoomPhase.GameOver;
        Dictionary<Guid, PlayerRankPair> finalRankings = room.GetRankOrder();

        ServerMessageEvent?.Invoke($"[{room.Code}] Game ended");

        foreach (ServerPlayer p in room.Players)
        {
            await BroadcastMessage(
                p.Client,
                PacketType.RoomInfo,
                RoomInfo.SendGameEnd(RoomActionType.GameEnd, room.Code, finalRankings));
        }
    }

    private async Task HandleDoodle(Packet packet, ServerPlayer player)
    {
        if (_rooms.TryGetValue(player.PlayerData.RoomCode, out Room? room))
        {
            DoodleInfo incomingDoodle = JsonSerializer.Deserialize<DoodleInfo>(packet.Content!)!;
            room.DoodleQueue.Enqueue(incomingDoodle);

            while (room.DoodleQueue.TryDequeue(out DoodleInfo? doodleInfo))
            {
                if (room.IsRoundStarted)
                    room.RoundDoodles.Enqueue(doodleInfo!);

                foreach (ServerPlayer p in room.Players)
                    if (p.PlayerData.GUID != player.PlayerData.GUID)
                        await BroadcastMessage(p.Client, PacketType.Doodle, doodleInfo!);
            }
        }
    }

    private async Task SendRoomStateSnapshotToPlayer(Room room, ServerPlayer player)
    {
        await BroadcastMessage(
            player.Client,
            PacketType.RoomInfo,
            RoomInfo.SendIsStarted(RoomActionType.Start, room.Code, room.IsGameStarted));

        ServerPlayer? currentTurnPlayer = room.Players
            .FirstOrDefault(p => p.PlayerData.GUID == room.CurrentTurnPlayerGuid);

        if (currentTurnPlayer is not null)
        {
            await BroadcastMessage(
                player.Client,
                PacketType.RoomInfo,
                RoomInfo.SendCurrentTurnPlayer(RoomActionType.SelectPlayer, room.Code, currentTurnPlayer.PlayerData));
        }

        await BroadcastMessage(
            player.Client,
            PacketType.RoomInfo,
            RoomInfo.SendRankingSnapshot(RoomActionType.RoomUpdate, room.Code, room.GetRankOrder()));

        switch (room.Phase)
        {
            case RoomPhase.ChoosingWord:
                await BroadcastMessage(
                    player.Client,
                    PacketType.RoomInfo,
                    RoomInfo.SendRoundSetup(RoomActionType.RoundStart, room.Code));
                break;

            case RoomPhase.Drawing:
                await BroadcastMessage(
                    player.Client,
                    PacketType.RoomInfo,
                    RoomInfo.SendDrawingStarted(RoomActionType.DrawingStarted, room.Code, room.CurrentWord, room.IsRoundStarted));

                foreach (RevealedLetters revealedLetter in room.RevealedLetters)
                {
                    await BroadcastMessage(
                        player.Client,
                        PacketType.RoomInfo,
                        RoomInfo.SendRandomLetterReveal(RoomActionType.RevealedLetter, room.Code, revealedLetter.Letter, revealedLetter.Index));
                }

                foreach (DoodleInfo doodleInfo in room.RoundDoodles)
                {
                    await BroadcastMessage(player.Client, PacketType.Doodle, doodleInfo);
                }
                break;

            case RoomPhase.RoundSummary:
                await BroadcastMessage(
                    player.Client,
                    PacketType.RoomInfo,
                    RoomInfo.SendRoundOver(RoomActionType.RoundEnd, room.Code, false, room.CurrentWord, room.GetRoundPoints(), room.EndedByTime));
                break;

            default:
                break;
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
