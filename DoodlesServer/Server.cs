using GameLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace DoodlesServer;

public class Server
{
    private const string DELIM = "<!EOM!>";

    private TcpListener listener;
    private List<TcpClient> clients = new();
    private List<Player> players = new();
    private bool isRunning;

    public delegate void ServerMessage(string message);
    public event ServerMessage? ServerMessageEvent;
    public event ServerMessage? LocalMessageEvent;
    public event ServerMessage? ConnectMessageEvent;
    public event ServerMessage? DisconnectMessageEvent;

    public Server(int port)
    {
        listener = new(IPAddress.Any, port);
    }

    public async Task Start()
    {
        listener.Start();
        isRunning = true;
        LocalMessageEvent?.Invoke($"Server Started {listener.LocalEndpoint}");

        while (isRunning)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            clients.Add(client);

            Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        Player player = new(client, "");
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

    private async Task ProcessPacket(Packet packet, Player player)
    {
        switch (packet.ContentType)
        {
            case MessageType.ServerOnly:
                break;

            case MessageType.Broadcast:
                break;

            case MessageType.Connect:
                await HandleConnect(packet, player);
                break;

            case MessageType.Disconnect:
                await HandleDisconnect(player);
                break;

            case MessageType.Doodle:
                await HandleDoodle();
                break;

            default:
                break;
        }
    }

    private async Task HandleConnect(Packet packet, Player player)
    {
        player.Username = (string)packet.Content;
        players.Add(player);

        ConnectMessageEvent?.Invoke($"[SERVER]: {player.Username} Connected ({player.Client.Client.RemoteEndPoint})");
        foreach (var client in clients)
            await BroadcastMessage(client, MessageType.Connect, $"{player.Username} Connected");
    }
    private async Task HandleDisconnect(Player player)
    {
        if (player.Username == "") return;

        DisconnectMessageEvent?.Invoke($"[SERVER]: {player.Username} Disconnected ({player.Client.Client.RemoteEndPoint})");
        foreach (var client in clients)
            await BroadcastMessage(client, MessageType.Disconnect, $"{player.Username} Disconnected");
    }

    private async Task HandleDoodle()
    {

    }

    public async Task BroadcastMessage(MessageType type, object content)
    {
        List<Task> sendTasks = clients.Select(client =>
            MessageFunctions.SendPacket(client, type, content)).ToList();

        await Task.WhenAll(sendTasks);

        //Gotchas in this implementation
        //  •	clients is a shared mutable list; if clients are added/ removed while broadcasting, this can throw or behave unpredictably.
        //  •	If any send task fails, Task.WhenAll faults(you’ll need try/catch if you want partial success behavior).
        //  •	The method sends to all entries in clients, even potentially disconnected ones unless cleanup is handled elsewhere.
    }

    public async Task BroadcastMessage(TcpClient client, MessageType type, object content)
    {
        //second method for testing
        await MessageFunctions.SendPacket(client, type, content);
    }
}
