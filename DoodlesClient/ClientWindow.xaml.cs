using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DoodlesClient;

/// <summary>
/// Interaction logic for ClientWindow.xaml
/// </summary>
public partial class ClientWindow : Window
{
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