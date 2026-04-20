using DoodlesClient.Models;
using GameLibrary.Core;
using GameLibrary.Enums;
using GameLibrary.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for ClientWindow.xaml
/// </summary>
public partial class ClientWindow : Window, INotifyPropertyChanged
{
    private readonly Client _client;
    private readonly string _username;
    private readonly string _roomCode;
    private Guid _currentTurnPlayerGuid;
    private string _currentTurnUsername;

    public ObservableCollection<RoundSummaryRow> RoundSummaryRows { get; } = [];
    public ObservableCollection<RoundSummaryRow> EndGameRows { get; } = [];

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

    private bool _isGameStarted;
    public bool IsGameStarted
    {
        get => _isGameStarted;
        set => SetField(ref _isGameStarted, value);
    }

    private bool _isRoundStarted;
    public bool IsRoundActive
    {
        get => _isRoundStarted;
        set => SetField(ref _isRoundStarted, value);
    }

    private bool _showWords;
    public bool ShowWords
    {
        get => _showWords;
        set => SetField(ref _showWords, value);
    }

    private bool _isHost;
    public bool IsHost
    {
        get => _isHost;
        set => SetField(ref _isHost, value);
    }

    private string _nonPlayerInfoText;
    public string NonPlayerInfoText
    {
        get => _nonPlayerInfoText;
        set => SetField(ref _nonPlayerInfoText, value);
    }

    private bool _isRoundSummaryVisible;
    public bool IsRoundSummaryVisible
    {
        get => _isRoundSummaryVisible;
        set => SetField(ref _isRoundSummaryVisible, value);
    }
    
    private string _roundEndWordText = "The word was";
    public string RoundEndWordText
    {
        get => _roundEndWordText;
        set => SetField(ref _roundEndWordText, value);
    }

    private string _roundEndSubText = string.Empty;
    public string RoundEndSubText
    {
        get => _roundEndSubText;
        set => SetField(ref _roundEndSubText, value);
    }

    private bool _isEndGameSummaryVisible;
    public bool IsEndGameSummaryVisible
    {
        get => _isEndGameSummaryVisible;
        set => SetField(ref _isEndGameSummaryVisible, value);
    }

    // Joined Room
    public ClientWindow(string username, string roomCode, Client client)
    {
        _client = client;
        _username = username;
        _roomCode = roomCode;
        IsHost = false;
        InitializeComponent();
    }

    // Created Room
    public ClientWindow(PlayerData data, Client client)
    {
        _client = client;
        _username = data.Username;
        _roomCode = data.RoomCode;
        IsHost = true;
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        BuildPaletteButtons();
        ResetBrushSettings();

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

        _client.RoomGameStartedEvent += Client_RoomGameStartedEvent;
        _client.RoomSelectPlayerEvent += Client_RoomSelectPlayerEvent;
        _client.RoomWordListEvent += Client_RoomWordListEvent;
        _client.RoomRoundStartEvent += Client_RoomRoundStartEvent;
        _client.RoomDrawingStartedEvent += Client_RoomDrawingStartedEvent;
        _client.RoomRevealLetterEvent += Client_RoomRevealLetterEvent;
        _client.RoomRoundEndEvent += Client_RoomRoundEndEvent;
        _client.RoomGameEndEvent += Client_RoomGameEndEvent;
        _client.RoomRankUpdateEvent += Client_RoomRankUpdateEvent;

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
        Dispatcher.Invoke(() =>
        {
            ClientDoodler.UserThickness = (int)doodleInfo.UserThickness!;
            ClientDoodler.UserColour = (Color)doodleInfo.UserColour!;
            ClientDoodler.StartStrokeAt((Point)doodleInfo.Point!);
        });
    }

    private void Client_MoveEvent(DoodleInfo doodleInfo)
    {
        Dispatcher.Invoke(() =>
        {
            ClientDoodler.UserThickness = (int)doodleInfo.UserThickness!;
            ClientDoodler.UserColour = (Color)doodleInfo.UserColour!;
            ClientDoodler.ContinueStrokeAt((Point)doodleInfo.Point!);
        });
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

    private void Client_RoomSelectPlayerEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            IsRoundSummaryVisible = false;
            IsEndGameSummaryVisible = false;

            _currentTurnPlayerGuid = info.CurrentTurnPlayer!.GUID;
            _currentTurnUsername = info.CurrentTurnPlayer!.Username;

            foreach (ClientPlayer player in _client.ConnectedPlayers)
                player.PlayerData.IsTurn = player.PlayerData.GUID == _currentTurnPlayerGuid;

            IsPlayerTurn = _client.CurrentClientPlayer?.PlayerData?.IsTurn ?? false;

            foreach (PlayerCardControl card in PlayerList.Children.OfType<PlayerCardControl>())
                card.UpdateIsDrawing(card.PlayerGuid == _currentTurnPlayerGuid);

            ListBoxItem playerIsDrawing = new();
            playerIsDrawing.FontWeight = FontWeights.Bold;
            playerIsDrawing.Content = $"{_currentTurnUsername} is doodling now!";
            playerIsDrawing.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9871B3"));

            MessagesLst.Items.Add(playerIsDrawing);
        });
    }

    private void Client_RoomGameStartedEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            if (info.IsGameStarted is true)
                IsGameStarted = true;

            IsEndGameSummaryVisible = false;
        });
    }

    private void Client_RoomWordListEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
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
        });
    }

    private async void WordBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            string chosenWord = (string)button.Content;
            await PacketHelper.SendPacketToServer(_client, PacketType.RoomInfo, RoomInfo.SendChosenWord(RoomActionType.ChosenWord, _roomCode, chosenWord));
            SetDoodlerBackground("White");
        }
    }

    private void SetWordHint(bool isPlayerTurn, string chosenWord)
    {
        Dispatcher.Invoke(() =>
        {
            WordHint.Children.Clear();

            if (isPlayerTurn)
            {
                WordHint.Children.Add(new TextBlock()
                {
                    Text = chosenWord,
                    Tag = chosenWord,
                    Margin = new Thickness(5, 5, 5, 0),
                    FontSize = 20,
                });
            }
            else
            {
                for (int i = 0; i < chosenWord.Length; i++)
                {
                    TextBlock block = new()
                    {
                        Text = chosenWord[i] == ' ' ? " " : "_",
                        Tag = i,
                        Margin = new Thickness(5, 5, 5, 0),
                        FontSize = 20,
                    };

                    WordHint.Children.Add(block);
                }
            }
        });
    }

    private void Client_RoomRoundStartEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            IsRoundSummaryVisible = false;
            IsEndGameSummaryVisible = false;
            ShowWords = true;
            ClientDoodler.RequestClear();
            ResetBrushSettings();

            if (!IsPlayerTurn)
                NonPlayerInfoText = $"Player {_currentTurnUsername} is choosing a word...";
        });
    }

    private void Client_RoomDrawingStartedEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            SetDoodlerBackground("White");
            IsRoundSummaryVisible = false;
            IsEndGameSummaryVisible = false;
            IsRoundActive = (bool)info.IsRoundActive!;
            ShowWords = false;

            foreach (var card in PlayerList.Children.OfType<PlayerCardControl>().ToList())
                card.UpdateColour("white");

            WordHint.Children.Clear();
            string chosenWord = info.ChosenWord!;

            SetWordHint(IsPlayerTurn, chosenWord);
        });
    }

    private void Client_RoomRevealLetterEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            if (!IsPlayerTurn)
            {
                TextBlock? block = WordHint.Children
                    .OfType<TextBlock>()
                    .FirstOrDefault(tb => tb.Tag is int idx && idx == info.RevealedLetterIndex);

                // AI: fix null issues
                block?.SetCurrentValue(TextBlock.TextProperty, info.RevealedLetter?.ToString());
            }
        });
    }

    private void Client_RoomRankUpdateEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            Dictionary<Guid, PlayerRankPair> dict = info.PlayerRankings!;

            foreach (ClientPlayer player in _client.ConnectedPlayers)
            {
                if (dict.TryGetValue(player.PlayerData.GUID, out PlayerRankPair? pair))
                    player.PlayerData.Score = pair.Score;
            }

            var allCards = PlayerList.Children.OfType<PlayerCardControl>().ToList();
            for (int i = 0; i < allCards.Count; i++)
            {
                PlayerRankPair pair = dict[allCards[i].PlayerGuid];
                allCards[i].UpdateRankAndScore(pair.Rank, pair.Score);
            }

            if (info.CorrectGuesser is null)
                return;

            PlayerCardControl cardToUpdateColour = allCards.FirstOrDefault(c => c.PlayerGuid == info.CorrectGuesser!.GUID)!;
            cardToUpdateColour.UpdateColour((cardToUpdateColour.CardIndex % 2 == 0)
                        ? "lightgreen"
                        : "green");

            ListBoxItem correctGuess = new();
            correctGuess.Content = $"{info.CorrectGuesser!.Username} guessed the word!";
            correctGuess.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#54FF83"));
            correctGuess.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E8C48"));

            MessagesLst.Items.Add(correctGuess);
        });
    }

    private void Client_RoomRoundEndEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            SetDoodlerBackground("grey");
            IsRoundActive = (bool)info.IsRoundActive!;

            RoundEndWordText = $"The word was {info.ChosenWord}";
            RoundEndSubText = (bool)info.EndedByTime! ? $"Time is up!" : "Everyone guessed it!";

            RoundSummaryRows.Clear();

            Dictionary<Guid, int> roundPoints = info.RoundPoints ?? [];

            var orderedRoundRows = _client.ConnectedPlayers
                .Select(player => new RoundSummaryRow
                {
                    Username = player.PlayerData.Username,
                    Score = roundPoints.TryGetValue(player.PlayerData.GUID, out int points) ? points : 0
                })
                .OrderByDescending(row => row.Score)
                .ThenBy(row => row.Username);

            foreach (RoundSummaryRow row in orderedRoundRows)
                RoundSummaryRows.Add(row);

            IsRoundSummaryVisible = true;
        });
    }

    private void Client_RoomGameEndEvent(RoomInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            SetDoodlerBackground("grey");
            IsRoundActive = false;
            IsGameStarted = false;
            ShowWords = false;

            IsRoundSummaryVisible = false;

            EndGameRows.Clear();
            Dictionary<Guid, string> usernames = _client.ConnectedPlayers
                .ToDictionary(p => p.PlayerData.GUID, p => p.PlayerData.Username);

            foreach (var ranking in info.PlayerRankings!.OrderBy(kvp => kvp.Value.Rank))
            {
                EndGameRows.Add(new RoundSummaryRow
                {
                    Username = usernames.TryGetValue(ranking.Key, out string? username) ? username : "Unknown",
                    Score = ranking.Value.Score
                });
            }

            IsEndGameSummaryVisible = true;
        });
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

    private void ResetBrushSettings()
    {
        ClientDoodler.RequestResetBrushSettings();

        var encodedHex = Uri.EscapeDataString(Doodler.DefaultBrushHex);
        SelectedColour.Source = new BitmapImage(new Uri($"pack://application:,,,/Images/Frames/frame_{encodedHex}.png"));
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
            IsGameStarted = true;
            await PacketHelper.SendPacketToServer(_client, PacketType.RoomInfo, RoomInfo.SendIsStarted(RoomActionType.Start, _roomCode, IsGameStarted));
        }
        else
        {
            MessageBox.Show("Room needs at least 2 players to start game!");
        }
    }

    private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
    {
        await PacketHelper.SendPacketToServer(_client, PacketType.Disconnect, _client.CurrentClientPlayer.PlayerData);
    }

    private async void SendBtn_Click(object sender, RoutedEventArgs e)
    {
        if (IsRoundActive)
            await PacketHelper.SendPacketToServer(_client, PacketType.RoomInfo, RoomInfo.SendGuess(RoomActionType.Guess, _roomCode, new Message(_username, MessageTxt.Text)));
        else
            await PacketHelper.SendPacketToServer(_client, PacketType.Message, new Message(_username, MessageTxt.Text));
        MessageTxt.Clear();
    }

    private void Client_ClientMessageEvent(string message)
    {
        Dispatcher.Invoke(() =>
        {
            MessagesLst.Items.Add(message);
        });
    }

    private void Client_DisconnectMessageEvent(string message, List<ClientPlayer> connectedPlayers)
    {
        Dispatcher.Invoke(() =>
        {
            MessagesLst.Items.Add(message);
            SetPlayerCard(connectedPlayers);
        });
    }

    private void Client_ConnectMessageEvent(string message, List<ClientPlayer> connectedPlayers)
    {
        Dispatcher.Invoke(() =>
        {
            ListBoxItem connectMessage = new();
            connectMessage.Content = message;
            connectMessage.FontWeight = FontWeights.Bold;
            connectMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F48CC"));
            MessagesLst.Items.Add(connectMessage);

            SetPlayerCard(connectedPlayers);
        });
    }

    private void SetPlayerCard(List<ClientPlayer> connectedPlayers)
    {
        PlayerList.Children.Clear();

        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            ClientPlayer player = connectedPlayers[i];
            PlayerCardControl card = new();
            card.SetInfo(i, player.PlayerData.GUID, player.PlayerData.IsHost, 0, player.PlayerData.Username, player.PlayerData.Score, false);

            PlayerList.Children.Add(card);
        }
    }

    private void SetDoodlerBackground(string colour)
    {
        ImageBrush brush = new()
        {
            ImageSource = new BitmapImage(new Uri($"pack://application:,,,/Images/doodler_border_{colour}.png", UriKind.Absolute)),
            Stretch = Stretch.Fill
        };
        brush.Freeze();
        DoodlerGrid.Background = brush;
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