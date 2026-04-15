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

    public List<int> PlayerCounts { get; set; } = [2, 3, 4, 5, 6, 7, 8, 9, 10];
    public int PlayerCount { get; set; } = 8;
    public List<int> DoodleTimes { get; set; } = [15, 20, 30, 40, 50, 60, 70, 80, 90, 100, 120];
    public int DoodleTime { get; set; } = 80;
    public List<int> Rounds { get; set; } = [1, 2, 3, 4, 5, 6];
    public int NumRounds { get; set; } = 3;

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
