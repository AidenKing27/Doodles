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
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    Window1 window1;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        window1 = new();
        window1.Show();

        drawingHost.DoodleMouseDownEvent += DrawingHost_MouseDownCustomEvent;
        drawingHost.DoodleMouseMoveEvent += DrawingHost_MouseMoveCustomEvent;
        drawingHost.DoodleMouseUpEvent += DrawingHost_MouseUpCustomEvent;
        drawingHost.DoodleUndoEvent += DrawingHost_UndoCustomEvent;
        drawingHost.DoodleClearEvent += DrawingHost_DoodleClearEvent;
    }

    private void DrawingHost_DoodleClearEvent()
    {
        window1.drawingHost.Clear();
    }

    private void DrawingHost_MouseDownCustomEvent(Point p)
    {
        window1.drawingHost.StartStrokeAt(p);
    }

    private void DrawingHost_MouseMoveCustomEvent(Point p)
    {
        window1.drawingHost.ContinueStrokeAt(p);
    }

    private void DrawingHost_MouseUpCustomEvent()
    {
        window1.drawingHost.EndStroke();
    }

    private void DrawingHost_UndoCustomEvent()
    {
        window1.drawingHost.Undo();
    }

    private void ThinBtn_Click(object sender, RoutedEventArgs e)
    {
        drawingHost.UserThickness = 3;
    }

    private void ThickBtn_Click(object sender, RoutedEventArgs e)
    {
        drawingHost.UserThickness = 10;
    }

    private void UndoBtn_Click(object sender, RoutedEventArgs e)
    {
        drawingHost.Undo();
    }
}