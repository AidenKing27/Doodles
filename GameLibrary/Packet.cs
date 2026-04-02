namespace GameLibrary;

public class Packet
{
    public MessageType ContentType { get; set; }
    public object Content { get; set; }
}
