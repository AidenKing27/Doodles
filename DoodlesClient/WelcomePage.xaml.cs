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
    private string _roomCode;

    public WelcomePage()
    {
        InitializeComponent();

        // AI: prevent pasting invalid characters
        DataObject.AddPastingHandler(UsernameTxt, UsernameTxt_Pasting);
        DataObject.AddPastingHandler(CodeTxt, CodeTxt_Pasting);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _client = new("172.18.28.126", 55555);
        //_client = new("172.18.28.134", 55555);
        _client.RoomListEvent += Client_RoomListEvent;

        await PacketHelper.SendPacketToServer(_client, PacketType.ServerConnect, new());
        await PacketHelper.SendPacketToServer(_client, PacketType.RoomCodes, new());
    }

    private void Client_RoomListEvent(List<string> roomCodes)
    {
        _roomCodes = roomCodes;
    }

    private async void JoinBtn_Click(object sender, RoutedEventArgs e)
    {
        await PacketHelper.SendPacketToServer(_client, PacketType.RoomCodes, new());
        if (_roomCodes.Contains(_roomCode))
        {
            ClientWindow clientWindow = new(_username, _roomCode, _client);
            clientWindow.Closed += ClientWindow_Closed;
            clientWindow.Show();
            Hide();
        }
        else
        {
            MessageBox.Show($"Room Code {_roomCode} does not exist! Create a room or join a valid room.");
            CodeTxt.Clear();
        }
    }

    private void CreateBtn_Click(object sender, RoutedEventArgs e)
    {
        CreateRoom createRoom = new(_client, _username);
        if (createRoom.ShowDialog() == true)
            Hide();
    }

    private void UsernameTxt_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Regex usernameRegex = new Regex("^[A-Za-z]+$");
        e.Handled = !usernameRegex.IsMatch(e.Text);
    }

    private void CodeTxt_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Regex codeRegex = new Regex("^[A-Za-z0-9]+$");
        e.Handled = !codeRegex.IsMatch(e.Text);
    }

    private void UsernameTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
        _username = UsernameTxt.Text;
        CheckCreateButton();
        CheckJoinButton();
    }

    private void CodeTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
        _roomCode = CodeTxt.Text;
        CheckJoinButton();
    }

    private void CheckJoinButton()
    {
        if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_roomCode))
            JoinBtn.IsEnabled = true;
        else
            JoinBtn.IsEnabled = false;
    }

    private void CheckCreateButton()
    {
        if (!string.IsNullOrEmpty(_username))
            CreateBtn.IsEnabled = true;
        else
            CreateBtn.IsEnabled = false;
    }

    private void UsernameTxt_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        // AI: prevent pasting invalid characters
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        string pastedText = (string)e.DataObject.GetData(DataFormats.Text)!;
        if (!Regex.IsMatch(pastedText, "^[A-Za-z]+$"))
            e.CancelCommand();
    }

    private void CodeTxt_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        // AI: prevent pasting invalid characters
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        string pastedText = (string)e.DataObject.GetData(DataFormats.Text)!;
        if (!Regex.IsMatch(pastedText, "^[A-Za-z0-9]+$"))
            e.CancelCommand();
    }

    private void ClientWindow_Closed(object? sender, EventArgs e) => Application.Current.Shutdown();
}
