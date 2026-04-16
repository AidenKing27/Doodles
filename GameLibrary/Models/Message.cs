namespace GameLibrary.Models;

public class Message
{
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public Message(string sender, string content)
    {
        Sender = sender;
        Content = content;
    }
}
