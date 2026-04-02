using GameLibrary;
using System.Windows;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for ClientWindow.xaml
/// </summary>
public partial class ClientWindow : Window
{
    private Client client;
    private Player player;

    public ClientWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Doodler.DoodleMouseDownEvent += DrawingHost_MouseDownCustomEvent;
        Doodler.DoodleMouseMoveEvent += DrawingHost_MouseMoveCustomEvent;
        Doodler.DoodleMouseUpEvent += DrawingHost_MouseUpCustomEvent;
        Doodler.DoodleUndoEvent += DrawingHost_UndoCustomEvent;
        Doodler.DoodleClearEvent += DrawingHost_DoodleClearEvent;

    }

    private void ConnectBtn_Click(object sender, RoutedEventArgs e)
    {
        client = new("172.18.31.102", 55555);
        client.ClientMessageEvent += Client_ClientMessageEvent;
        client.ConnectMessageEvent += Client_ConnectMessageEvent;
        client.DisconnectMessageEvent += Client_DisconnectMessageEvent;

        _ = client.SendMessage(ContentType.Connect, new PlayerData("Aiden"));
    }

    private void SendBtn_Click(object sender, RoutedEventArgs e)
    {
        _ = client.SendMessage(ContentType.Message, MessageTxt.Text);
    }

    private void Client_ClientMessageEvent(string message)
    {
        throw new NotImplementedException();
    }

    private void Client_DisconnectMessageEvent(string message, List<GamePlayer> connectedPlayers)
    {
        throw new NotImplementedException();
    }

    private void Client_ConnectMessageEvent(string message, List<GamePlayer> connectedPlayers)
    {
        throw new NotImplementedException();
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