using GameLibrary.Models;
using GameLibrary.Enums;
using System.Windows;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for ClientWindow.xaml
/// </summary>
public partial class ClientWindow : Window
{
    private readonly Client _client;
    private readonly string _username;
    private readonly string _roomCode;

    public ClientWindow(string username, string roomCode, Client client)
    {
        InitializeComponent();
        _username = username;
        _roomCode = roomCode;
        _client = client;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Doodler.DoodleMouseDownEvent += DrawingHost_MouseDownCustomEvent;
        Doodler.DoodleMouseMoveEvent += DrawingHost_MouseMoveCustomEvent;
        Doodler.DoodleMouseUpEvent += DrawingHost_MouseUpCustomEvent;
        Doodler.DoodleUndoEvent += DrawingHost_UndoCustomEvent;
        Doodler.DoodleClearEvent += DrawingHost_DoodleClearEvent;

        _client.ClientMessageEvent += Client_ClientMessageEvent;
        _client.ConnectMessageEvent += Client_ConnectMessageEvent;
        _client.DisconnectMessageEvent += Client_DisconnectMessageEvent;

        _ = _client.SendMessage(ContentType.PlayerData, new PlayerData(_username, _roomCode));
    }
    private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
    {
        _ = _client.SendMessage(ContentType.Disconnect, _client.Player.PlayerData.Username);
    }

    private void SendBtn_Click(object sender, RoutedEventArgs e)
    {
        _ = _client.SendMessage(ContentType.Message, MessageTxt.Text);
        MessageTxt.Clear();
    }

    private void Client_ClientMessageEvent(string message)
    {
        Dispatcher.Invoke(() => MessagesLst.Items.Add(message));
    }

    private void Client_DisconnectMessageEvent(string message, List<ClientPlayer> connectedPlayers)
    {
        Dispatcher.Invoke(() => MessagesLst.Items.Add(message));
    }

    private void Client_ConnectMessageEvent(string message, List<ClientPlayer> connectedPlayers)
    {
        Dispatcher.Invoke(() => MessagesLst.Items.Add(message));
    }

    private void DrawingHost_DoodleClearEvent()
    {
    }

    private void DrawingHost_MouseDownCustomEvent(Point p)
    {
    }

    private void DrawingHost_MouseMoveCustomEvent(Point p)
    {
    }

    private void DrawingHost_MouseUpCustomEvent()
    {
    }

    private void DrawingHost_UndoCustomEvent()
    {
    }

    private void ThinBtn_Click(object sender, RoutedEventArgs e) => Doodler.UserThickness = 3;
    private void ThickBtn_Click(object sender, RoutedEventArgs e) => Doodler.UserThickness = 10;
    private void UndoBtn_Click(object sender, RoutedEventArgs e) => Doodler.Undo();

    
}