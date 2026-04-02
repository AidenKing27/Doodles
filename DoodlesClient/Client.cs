using GameLibrary;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DoodlesClient;

public class Client
{
    public async Task SendMessage(TcpClient client, MessageType type, object content)
    {
        await MessageFunctions.SendPacket(client, type, content);
    }
}
