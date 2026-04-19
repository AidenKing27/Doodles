using DoodlesClient.Models;
using GameLibrary.Core;
using GameLibrary.Enums;
using GameLibrary.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for ClientWindow.xaml
/// </summary>
public partial class ClientWindow : Window, INotifyPropertyChanged
{
    private readonly Client _client;
    private readonly string _username;
    private readonly string _roomCode;

    public bool IsPlayerTurn
    {
        get => _client.CurrentClientPlayer?.PlayerData?.IsTurn ?? false;
        set
        {
            var playerData = _client.CurrentClientPlayer?.PlayerData;
            if (playerData is null) return;

            if (playerData.IsTurn != value)
                playerData.IsTurn = value;

            OnPropertyChanged();
        }
    }

    private bool _isStarted;
    public bool IsStarted
    {
        get => _isStarted;
        set => SetField(ref _isStarted, value);
    }

    private bool _isHost;
    public bool IsHost
    {
        get => _isHost;
        set => SetField(ref _isHost, value);
    }

    // Joined Room
    public ClientWindow(string username, string roomCode, Client client)
    {
        InitializeComponent();
        _client = client;
        _username = username;
        _roomCode = roomCode;
        IsHost = false;
    }

    // Created Room
    public ClientWindow(PlayerData data, Client client)
    {
        InitializeComponent();
        _client = client;
        _username = data.Username;
        _roomCode = data.RoomCode;
        IsHost = true;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
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

        _client.RoomStartedEvent += Client_RoomStartedEvent;
        _client.RoomSelectPlayerEvent += Client_RoomSelectPlayerEvent;
        _client.RoomWordsEvent += Client_RoomWordsEvent;
        _client.RoomRoundStartEvent += Client_RoomRoundStartEvent;
        _client.RoomRevealLetterEvent += Client_RoomRevealLetterEvent;
        _client.RoomRoundEndEvent += Client_RoomRoundEndEvent;

        // Joined Room
        if (!IsHost)
        {
            PlayerData data = new(_username, _roomCode, IsHost);
            await PacketHelper.SendPacketToServer(_client, PacketType.PlayerData, data);
            await PacketHelper.SendPacketToServer(_client, PacketType.JoinRoom, data);
        }
    }

    private async void ClientDoodler_DoodleMouseDownEvent(Point p)
    {
        await PacketHelper.SendPacketToServer(
            _client, PacketType.Doodle, 
            DoodleInfo.SendDoodleInfo(DoodleType.Down, 
            ClientDoodler.UserThickness, ClientDoodler.UserColour, p));
    }

    private async void ClientDoodler_DoodleMouseMoveEvent(Point p)
    {
        await PacketHelper.SendPacketToServer(
            _client, 
            PacketType.Doodle, 
            DoodleInfo.SendDoodleInfo(DoodleType.Move, ClientDoodler.UserThickness, ClientDoodler.UserColour, p));
    }

    private async void ClientDoodler_DoodleMouseUpEvent()
    {
        await PacketHelper.SendPacketToServer(
            _client, 
            PacketType.Doodle, 
            DoodleInfo.SendDoodleInfo(DoodleType.Up, ClientDoodler.UserThickness, ClientDoodler.UserColour));
    }

    private async void ClientDoodler_DoodleUndoEvent()
    {
        await PacketHelper.SendPacketToServer(
            _client, 
            PacketType.Doodle, 
            DoodleInfo.SendDoodleInfo(DoodleType.Undo, ClientDoodler.UserThickness, ClientDoodler.UserColour));
    }

    private async void ClientDoodler_DoodleClearEvent()
    {
        await PacketHelper.SendPacketToServer(
            _client, 
            PacketType.Doodle, 
            DoodleInfo.SendDoodleInfo(DoodleType.Clear, ClientDoodler.UserThickness, ClientDoodler.UserColour));
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

    private void Client_RoomStartedEvent(RoomInfo info)
    {
        if (info.IsStarted is true)
        {
            Dispatcher.Invoke(() =>
            {
                IsStarted = true;
                DoodlerGrid.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(Doodler.Palette["White"]));
            });
        }
    }

    private void Client_RoomSelectPlayerEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            string currentTurnUsername = info.CurrentTurnPlayer!.Username;

            foreach (var player in _client.ConnectedPlayers)
                player.PlayerData.IsTurn = player.PlayerData.Username == currentTurnUsername;

            IsPlayerTurn = _client.CurrentClientPlayer?.PlayerData?.IsTurn ?? false;

            foreach (var card in PlayerList.Children.OfType<PlayerCardControl>())
                card.UpdateIsDrawing(card.Username == currentTurnUsername);
        });
    }

    private void Client_RoomWordsEvent(RoomInfo info)
    {
        SelectWordPanel.Children.Clear();
        foreach (string word in info.WordList!)
        {
            ImageBrush brush = new()
            {
                ImageSource = new BitmapImage(new Uri($"pack://application:,,,/Images/frame_long.png", UriKind.Absolute)),
                Stretch = Stretch.Fill
            };
            Button btn = new()
            {
                Content = word,
                Style = (Style?)FindResource("DoodleButtonStyle"),
                Margin = new Thickness(20, 0, 20, 0),
                Padding = new Thickness(30, 10, 30, 10),
                FontSize = 20,
                Background = brush
            };

            btn.Click += WordBtn_Click;
            SelectWordPanel.Children.Add(btn);
        }
    }

    private async void WordBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            await PacketHelper.SendPacketToServer(_client, PacketType.RoomInfo, RoomInfo.SendChosenWord(RoomActionType.ChosenWord, _roomCode, (string)button.Content));
    }

    private void Client_RoomRoundStartEvent(RoomInfo info)
    {
        WordHint.Children.Clear();

        for (int i = 0; i < info.WordLength; i++)
        {
            TextBlock block = new()
            {
                Text = "_",
                Tag = i,
                Margin = new Thickness(5, 0, 5, 0),
                FontSize = 20,
            };

            WordHint.Children.Add(block);
        }
    }

    private void Client_RoomRevealLetterEvent(RoomInfo info)
    {
        TextBlock block = WordHint.Children.OfType<TextBlock>().FirstOrDefault(b => int.Parse((string)b.Tag) == info.RevealedLetterIndex)!;
        block.Text = info.RevealedLetter.ToString();
    }

    private void Client_RoomRoundEndEvent(RoomInfo info)
    {
        throw new NotImplementedException();
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

    private async void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client.ConnectedPlayers.Count > 1)
        {
            IsStarted = true;
            DoodlerGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Doodler.Palette["White"]));

            await PacketHelper.SendPacketToServer(_client, PacketType.RoomInfo, RoomInfo.SendIsStarted(RoomActionType.Start, _roomCode, IsStarted));
        }
        else
        {
            MessageBox.Show("Room needs at least 2 players to start game!");
        }
    }

    private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
    {
        await PacketHelper.SendPacketToServer(_client, PacketType.Disconnect, _client.CurrentClientPlayer.PlayerData.Username);
    }

    private async void SendBtn_Click(object sender, RoutedEventArgs e)
    {
        await PacketHelper.SendPacketToServer(_client, PacketType.Message, new Message(_username, MessageTxt.Text));
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

        foreach (ClientPlayer player in connectedPlayers)
        {
            PlayerCardControl card = new();
            card.SetInfo(player.PlayerData.IsHost, 0, player.PlayerData.Username, player.PlayerData.Score, false);

            PlayerList.Children.Add(card);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}