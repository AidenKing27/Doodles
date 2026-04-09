using GameLibrary.Models;
using GameLibrary.Enums;
using System.Windows;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for ClientWindow.xaml
/// </summary>
public partial class ClientWindow : Window
{
    private Client client;
    private readonly string username;
    private readonly string roomCode;

    public ClientWindow(string username, string roomCode)
    {
        InitializeComponent();
        this.username = username;
        this.roomCode = roomCode;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Doodler.DoodleMouseDownEvent += DrawingHost_MouseDownCustomEvent;
        Doodler.DoodleMouseMoveEvent += DrawingHost_MouseMoveCustomEvent;
        Doodler.DoodleMouseUpEvent += DrawingHost_MouseUpCustomEvent;
        Doodler.DoodleUndoEvent += DrawingHost_UndoCustomEvent;
        Doodler.DoodleClearEvent += DrawingHost_DoodleClearEvent;

        ConnectToRoom();
    }

    private void ConnectToRoom()
    {
        client = new("localhost", 55555);
        client.ClientMessageEvent += Client_ClientMessageEvent;
        client.ConnectMessageEvent += Client_ConnectMessageEvent;
        client.DisconnectMessageEvent += Client_DisconnectMessageEvent;

        _ = client.SendMessage(ContentType.Connect, new PlayerData(username, roomCode));
    }

    private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
    {
        _ = client.SendMessage(ContentType.Disconnect, client.Player.PlayerData.Username);
    }

    private void SendBtn_Click(object sender, RoutedEventArgs e)
    {
        _ = client.SendMessage(ContentType.Message, MessageTxt.Text);
    }

    private void Client_ClientMessageEvent(string message)
    {
        Dispatcher.Invoke(() => MessagesLst.Items.Add(message));
    }

    private void Client_DisconnectMessageEvent(string message, List<GamePlayer> connectedPlayers)
    {
        Dispatcher.Invoke(() => MessagesLst.Items.Add(message));
    }

    private void Client_ConnectMessageEvent(string message, List<GamePlayer> connectedPlayers)
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