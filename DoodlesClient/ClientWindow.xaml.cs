using GameLibrary.Models;
using GameLibrary.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameLibrary.Core;
using System.Windows.Media.Imaging;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for ClientWindow.xaml
/// </summary>
public partial class ClientWindow : Window
{
    private readonly Client _client;
    private readonly string _username;
    private readonly string _roomCode;
    private readonly PlayerData? _data;

    public ClientWindow(string username, string roomCode, Client client)
    {
        InitializeComponent();
        _client = client;
        _username = username;
        _roomCode = roomCode;
    }

    public ClientWindow(PlayerData data, Client client)
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

        ClientDoodler.DoodleMouseDownEvent += ClientDoodler_DoodleMouseDownEvent;
        ClientDoodler.DoodleMouseMoveEvent += ClientDoodler_DoodleMouseMoveEvent;
        ClientDoodler.DoodleMouseUpEvent += ClientDoodler_DoodleMouseUpEvent;
        ClientDoodler.DoodleUndoEvent += ClientDoodler_DoodleUndoEvent;
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

    private void ClientDoodler_DoodleMouseDownEvent(Point p)
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Down, ClientDoodler.UserThickness, ClientDoodler.UserColour, p));
    }

    private void ClientDoodler_DoodleMouseMoveEvent(Point p)
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Move, ClientDoodler.UserThickness, ClientDoodler.UserColour, p));
    }

    private void ClientDoodler_DoodleMouseUpEvent()
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Up, ClientDoodler.UserThickness, ClientDoodler.UserColour));
    }

    private void ClientDoodler_DoodleUndoEvent()
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Undo, ClientDoodler.UserThickness, ClientDoodler.UserColour));
    }

    private void ClientDoodler_DoodleClearEvent()
    {
        PacketHelper.SendPacketToServer(_client, PacketType.Doodle, PacketHelper.CreateDoodleInfo(DoodleType.Clear, ClientDoodler.UserThickness, ClientDoodler.UserColour));
    }

    private void Client_DownEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(() => ClientDoodler.UserThickness = (int)doodleInfo.UserThickness!);
        Dispatcher.Invoke(() => ClientDoodler.UserColour = (Color)doodleInfo.UserColour!);
        Dispatcher.Invoke(() => ClientDoodler.StartStrokeAt((Point)doodleInfo.Point!));
    }

    private void Client_MoveEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(() => ClientDoodler.UserThickness = (int)doodleInfo.UserThickness!);
        Dispatcher.Invoke(() => ClientDoodler.UserColour = (Color)doodleInfo.UserColour!);
        Dispatcher.Invoke(() => ClientDoodler.ContinueStrokeAt((Point)doodleInfo.Point!));
    }

    private void Client_UpEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(ClientDoodler.EndStroke);
    }

    private void Client_UndoEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(ClientDoodler.PerformUndo);
    }

    private void Client_ClearEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(ClientDoodler.PerformClear);
    }

    private void BuildPaletteButtons()
    {
        ColourPalettePanel.Children.Clear();

        foreach (var colour in Doodler.Palette)
        {
            var encodedHex = Uri.EscapeDataString(colour.Value);
            ImageBrush brush = new()
            {
                ImageSource = new BitmapImage(new Uri($"pack://application:,,,/Images/Frames/Rotated/frame_{encodedHex}.png", UriKind.Absolute)),
                Stretch = Stretch.Fill
            };
            brush.Freeze();

            Button colourButton = new()
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(2),
                Style = (Style?)FindResource("DoodleButtonStyle"),
                Background = brush,
                Tag = colour.Value,
                ToolTip = colour.Key
            };

            colourButton.Click += PaletteButton_Click;
            ColourPalettePanel.Children.Add(colourButton);
        }
    }

    private void PaletteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var encodedHex = Uri.EscapeDataString((string)button.Tag);
            ClientDoodler.UserColour = (Color)ColorConverter.ConvertFromString((string)button.Tag);
            SelectedColour.Source = new BitmapImage(new Uri($"pack://application:,,,/Images/Frames/frame_{encodedHex}.png"));
        }
    }

    private void SetThickness_Event(object sender, RoutedEventArgs e)
    {
        ClientDoodler.RequestSetThickness(int.Parse((string)((Button)sender).Tag));
    }

    private void EraseBtn_Click(object sender, RoutedEventArgs e)
    {
        bool isErasing = true;
        ClientDoodler.RequestPencilErase(isErasing);
    }

    private void PencilBtn_Click(object sender, RoutedEventArgs e)
    {
        bool isPencil = false;
        ClientDoodler.RequestPencilErase(isPencil);
    }

    private void UndoBtn_Click(object sender, RoutedEventArgs e)
    {
        ClientDoodler.RequestUndo();
    }

    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        ClientDoodler.RequestClear();
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

    private void Client_ClientMessageEvent(string message)
    {
        Dispatcher.Invoke(() => MessagesLst.Items.Add(message));
    }

    private void Client_DisconnectMessageEvent(string message, List<ClientPlayer> connectedPlayers)
    {
        Dispatcher.Invoke(() => MessagesLst.Items.Add(message));
        Dispatcher.Invoke(() => SetPlayerCard(connectedPlayers));
    }

    private void Client_ConnectMessageEvent(string message, List<ClientPlayer> connectedPlayers)
    {
        Dispatcher.Invoke(() => MessagesLst.Items.Add(message));
        Dispatcher.Invoke(() => SetPlayerCard(connectedPlayers));
    }

    private void SetPlayerCard(List<ClientPlayer> connectedPlayers)
    {
        PlayerList.Children.Clear();

        foreach (var player in connectedPlayers)
        {
            PlayerCardControl card = new();
            card.SetInfo(0, player.PlayerData.Username, player.PlayerData.Score, true);

            PlayerList.Children.Add(card);
        }
    }
}