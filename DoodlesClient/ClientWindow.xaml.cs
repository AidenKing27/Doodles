using GameLibrary.Models;
using GameLibrary.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameLibrary.Core;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for ClientWindow.xaml
/// </summary>
public partial class ClientWindow : Window
{
    private readonly Client _client;
    private readonly string _username;
    private readonly string _roomCode;
    private readonly PlayerData _data;

    public ClientWindow(string username, string roomCode, Client client)
    {
        InitializeComponent();
        _client = client;
        _username = username;
        _roomCode = roomCode;
    }

    public ClientWindow(Client client, PlayerData data)
    {
        InitializeComponent();
        _data = data;
        _client = client;
        _username = data.Username;
        _roomCode = data.RoomCode;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        BuildPaletteButtons();

        ClientDoodler.DoodleMouseDownEvent += ClientDoodler_MouseDownCustomEvent;
        ClientDoodler.DoodleMouseMoveEvent += ClientDoodler_MouseMoveCustomEvent;
        ClientDoodler.DoodleMouseUpEvent += ClientDoodler_MouseUpCustomEvent;
        ClientDoodler.DoodleUndoEvent += ClientDoodler_UndoCustomEvent;
        ClientDoodler.DoodleClearEvent += ClientDoodler_DoodleClearEvent;

        _client.ClientMessageEvent += Client_ClientMessageEvent;
        _client.ConnectMessageEvent += Client_ConnectMessageEvent;
        _client.DisconnectMessageEvent += Client_DisconnectMessageEvent;
        _client.DownEvent += Client_DownEvent;
        _client.MoveEvent += Client_MoveEvent;
        _client.UpEvent += Client_UpEvent;
        _client.UndoEvent += Client_UndoEvent;
        _client.ClearEvent += Client_ClearEvent;

        if (_data is null)
            PacketHelper.SendPacketToServer(_client, PacketType.PlayerData, new PlayerData(_username, _roomCode));
    }

    private void Client_DownEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(() => ClientDoodler.StartStrokeAt((Point)doodleInfo.Point!));
    }

    private void Client_MoveEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(() => ClientDoodler.ContinueStrokeAt((Point)doodleInfo.Point!));
    }

    private void Client_UpEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(() => ClientDoodler.EndStroke());
    }

    private void Client_UndoEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(() => ClientDoodler.Undo());
    }

    private void Client_ClearEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(() => ClientDoodler.Clear());
    }

    private void BuildPaletteButtons()
    {
        ColourPalettePanel.Children.Clear();

        foreach (var colour in Doodler.Palette)
        {
            SolidColorBrush brush = new(colour.Value);
            brush.Freeze();

            Button colourButton = new()
            {
                Width = 25,
                Height = 25,
                Margin = new Thickness(2),
                Background = brush,
                Tag = colour.Value,
                ToolTip = colour.Key
            };

            colourButton.Click += PaletteButton_Click;
            ColourPalettePanel.Children.Add(colourButton);
        }
    }

    private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Disconnect, _client.CurrentClientPlayer.PlayerData.Username);
    }

    private void SendBtn_Click(object sender, RoutedEventArgs e)
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Message, new Message(_username, MessageTxt.Text));
        MessageTxt.Clear();
    }

    private void ClientDoodler_MouseDownCustomEvent(Point p)
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Down, ClientDoodler.UserThickness, ClientDoodler.UserColour, p));
    }

    private void ClientDoodler_MouseMoveCustomEvent(Point p)
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Move, ClientDoodler.UserThickness, ClientDoodler.UserColour, p));
    }

    private void ClientDoodler_MouseUpCustomEvent()
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Up, ClientDoodler.UserThickness, ClientDoodler.UserColour));
    }

    private void ClientDoodler_UndoCustomEvent()
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Undo, ClientDoodler.UserThickness, ClientDoodler.UserColour));
    }

    private void ClientDoodler_DoodleClearEvent()
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Clear, ClientDoodler.UserThickness, ClientDoodler.UserColour));
    }

    private void UndoBtn_Click(object sender, RoutedEventArgs e) => ClientDoodler.Undo();

    private void PaletteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            ClientDoodler.UserColour = (Color)button.Tag;
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
}