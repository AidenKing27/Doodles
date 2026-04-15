using GameLibrary.Enums;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for WelcomePage.xaml
/// </summary>
public partial class WelcomePage : Window
{
    private Client _client;
    private List<string> _roomCodes;
    private string _username;

    public WelcomePage()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _client = new("localhost", 55555);
        _client.RoomListEvent += Client_RoomListEvent;

        _ = _client.SendMessage(ContentType.Connect, new());
    }

    private void Client_RoomListEvent(List<string> roomCodes)
    {
        _roomCodes = roomCodes;
    }

    private void JoinBtn_Click(object sender, RoutedEventArgs e)
    {
        string roomCode = CodeTxt.Text;

        if (_roomCodes.Contains(roomCode))
        {
            ClientWindow clientWindow = new(this._username, roomCode, _client);
            clientWindow.Closed += ClientWindow_Closed;
            clientWindow.Show();
            Hide();
        }
        else
        {
            MessageBox.Show($"Room Code {roomCode} does not exist! Create a room or join a valid room.");
        }
    }

    private void ClientWindow_Closed(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void CreateBtn_Click(object sender, RoutedEventArgs e)
    {
        CreateRoom createRoom = new(_username, _client);
        createRoom.ShowDialog();
    }

    private void UsernameTxt_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("^[A-Za-z]+$");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void UsernameTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
        _username = UsernameTxt.Text;
    }
}
