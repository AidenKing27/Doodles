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
    DrawingInfo di;

    public MainWindow()
    {
        InitializeComponent();
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

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        di = new()
        {
            UserThickness = drawingHost.UserThickness,
            UserBrush = drawingHost.UserBrush,
            Visuals = drawingHost._visuals.ToList(),
            CurrentStroke = drawingHost._currentStroke,
            CurrentPoints = drawingHost._currentPoints.ToList()
        };

        drawingHost.Clear();
        //drawingHost = new();
    }

    private void PasteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (di == null) return;

        drawingHost.UserThickness = di.UserThickness;
        drawingHost.UserBrush = di.UserBrush;

        // Clear any existing visuals and re-add the saved visuals using AddVisual
        drawingHost.Clear();
        foreach (var v in di.Visuals)
            drawingHost.AddVisual(v);

        drawingHost._currentStroke = di.CurrentStroke;
        drawingHost._currentPoints = di.CurrentPoints.ToList(); // copy again to avoid shared list
    }
}