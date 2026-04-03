using GameLibrary;
using System;
using System.Collections.Generic;
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

    public GamePlayer Player { get; set; }
    private TcpClient client;
    private static List<GamePlayer> connectedPlayers = new();
    //private static Queue<GamePlayer> playerQueue;

    public delegate void ClientMessageHandler(string message);
    public event ClientMessageHandler? ClientMessageEvent;

    public delegate void ClientPlayerHandler(string message, List<GamePlayer> connectedPlayers);
    public event ClientPlayerHandler? ConnectMessageEvent;
    public event ClientPlayerHandler? DisconnectMessageEvent;

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
        switch (packet.ContentType)
        {
            case ContentType.Message:
                await HandleMessage(packet);
                break;

            case ContentType.Connect:
                await HandleConnect(packet);
                break;

            case ContentType.Disconnect:
                await HandleDisconnect(packet);
                break;

            case ContentType.Doodle:
                await HandleDoodle();
                break;

            default:
                break;
        }
    }

    private async Task HandleMessage(Packet packet)
    {
        ClientMessageEvent?.Invoke($"{Player.PlayerData.Username}: {JsonSerializer.Deserialize<string>(packet.Content!)}");
    }

    private async Task HandleConnect(Packet packet)
    {
        PlayerData newPlayerData = JsonSerializer.Deserialize<PlayerData>(packet.Content!)!;
        if (!connectedPlayers.Any(p => p.PlayerData.Username == newPlayerData.Username))
        {
            bool isPlayer = newPlayerData.Username == WelcomePage.Username;
            GamePlayer gamePlayer = new(newPlayerData, isPlayer);
            connectedPlayers.Add(gamePlayer);
            ConnectMessageEvent?.Invoke($"{gamePlayer.PlayerData.Username} joined the room!", [.. connectedPlayers]);
            if (isPlayer)
                Player = gamePlayer;
        }
    }

    private async Task HandleDisconnect(Packet packet)
    {
        GamePlayer? disconnectingUser = connectedPlayers.FirstOrDefault(p => p.PlayerData.Username == JsonSerializer.Deserialize<string>(packet.Content!));
        if (disconnectingUser != null)
        {
            connectedPlayers.Remove(disconnectingUser);
            DisconnectMessageEvent?.Invoke($"{disconnectingUser.PlayerData.Username} left the room!", [.. connectedPlayers]);
        }
    }

    private async Task HandleDoodle()
    {

    }

    public async Task SendMessage(ContentType type, object content)
    {
        await MessageFunctions.SendPacket(client, MessageFunctions.CreatePacket(type, content));
    }
}
