using GameLibrary.Enums;
using GameLibrary.Models;
using System.Text;
using System.Windows;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for CreateRoom.xaml
/// </summary>
public partial class CreateRoom : Window
{
    private Client _client;
    private string _username;

    public List<int> PlayerCounts { get; set; } = [2, 3, 4, 5, 6, 7, 8, 9, 10];
    public int PlayerCount { get; set; } = 8;
    public List<int> DoodleTimes { get; set; } = [15, 20, 30, 40, 50, 60, 70, 80, 90, 100, 120];
    public int DoodleTime { get; set; } = 80;
    public List<int> Rounds { get; set; } = [1, 2, 3, 4, 5, 6];
    public int NumRounds { get; set; } = 3;

    public CreateRoom(Client client, string username)
    {
        InitializeComponent();
        _client = client;
        _username = username;
    }

    private void CreateBtn_Click(object sender, RoutedEventArgs e)
    {
        string roomCode = GenerateRoomCode();
        PlayerData data = new PlayerData(_username, roomCode);
        PacketHelper.SendPacketToServer(_client, PacketType.PlayerData, data);
        PacketHelper.SendPacketToServer(_client, PacketType.CreateRoom, new Room(roomCode, PlayerCount, DoodleTime, NumRounds));

        ClientWindow clientWindow = new(_client, data);
        clientWindow.Closed += ClientWindow_Closed;
        clientWindow.Show();
        DialogResult = true;
        Hide();
    }

    private string GenerateRoomCode()
    {
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Random rnd = new();
        StringBuilder sb = new();

        for (int i = 0; i < 5; i++)
            sb.Append(chars[rnd.Next(chars.Length)]);
        return sb.ToString();
    }

    private void ClientWindow_Closed(object? sender, EventArgs e) => Application.Current.Shutdown();
}
