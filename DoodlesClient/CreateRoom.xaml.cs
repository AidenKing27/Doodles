using GameLibrary.Enums;
using GameLibrary.Models;
using System.Windows;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for CreateRoom.xaml
/// </summary>
public partial class CreateRoom : Window
{
    private Client _client;
    private readonly string _username;

    private string _roomCode;
    private int _playerCount;
    private int _drawtime;
    private int _rounds;

    public CreateRoom(string username, Client client)
    {
        InitializeComponent();
        _username = username;
        _client = client;
    }

    private void CreateBtn_Click(object sender, RoutedEventArgs e)
    {
        //Room newRoom = new();
        //_ = _client.SendMessage(ContentType.CreateRoom, newRoom);
    }
}
