using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for PlayerCardControl.xaml
/// </summary>
public partial class PlayerCardControl : UserControl, INotifyPropertyChanged
{
    public int CardIndex { get; set; }
    public Guid PlayerGuid { get; set; }

    private string _rank = "#0";
    private string _username = "Username";
    private string _score = "Points: 0";
    private bool _isDrawing;
    private bool _isHost;

    public string Rank
    {
        get => _rank;
        set => SetField(ref _rank, value);
    }

    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    public string Score
    {
        get => _score;
        set => SetField(ref _score, value);
    }

    public bool IsDrawing
    {
        get => _isDrawing;
        set => SetField(ref _isDrawing, value);
    }

    public bool IsHost
    {
        get => _isHost;
        set => SetField(ref _isHost, value);
    }

    public PlayerCardControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void SetInfo(int cardIndex, Guid playerGuid, bool isHost, int rank, string username, int score, bool isDrawing)
    {
        CardIndex = cardIndex;
        PlayerGuid = playerGuid;
        IsHost = isHost;
        Rank = $"#{rank}";
        Username = username;
        Score = $"Points: {score}";
        IsDrawing = isDrawing;
    }

    public void UpdateIsDrawing(bool isDrawing)
    {
        IsDrawing = isDrawing;
    }

    public void UpdateRankAndScore(int rank, int score)
    {
        Rank = $"#{rank}";
        Score = $"Points: {score}";
    }

    public void UpdateColour(string colour)
    {
        ImageBrush brush = new()
        {
            ImageSource = new BitmapImage(new Uri($"pack://application:,,,/Images/playercard_{colour}.png", UriKind.Absolute)),
            Stretch = Stretch.Fill
        };
        brush.Freeze();
        Background = brush;
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
