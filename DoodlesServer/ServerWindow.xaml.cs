using System.Windows;

namespace DoodlesServer;

/// <summary>
/// Interaction logic for ServerWindow.xaml
/// </summary>
public partial class ServerWindow : Window
{
    public ServerWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Server srv = new(55555);
        srv.ServerMessageEvent += Srv_ServerMessageEvent;
        srv.ConnectMessageEvent += Srv_ConnectMessageEvent;
        srv.DisconnectMessageEvent += Srv_DisconnectMessageEvent;

        _ = srv.Start();
    }

    private void Srv_ServerMessageEvent(string message)
    {
        Dispatcher.Invoke(() => ConsoleLst.Items.Add(message));
    }

    private void Srv_DisconnectMessageEvent(string message)
    {
        Dispatcher.Invoke(() => ConsoleLst.Items.Add(message));
    }

    private void Srv_ConnectMessageEvent(string message)
    {
        Dispatcher.Invoke(() => ConsoleLst.Items.Add(message));
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}