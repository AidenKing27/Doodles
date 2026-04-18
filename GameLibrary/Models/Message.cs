namespace GameLibrary.Models;

public class Message(string sender, string content)
{
    public string Sender { get; set; } = sender;
    public string Content { get; set; } = content;
}
