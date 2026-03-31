using System.Windows;
using System.Windows.Media;

namespace DoodlesClient;

public class DrawingInfo
{
    public int UserThickness { get; set; } = 3;
    public Brush UserBrush { get; set; } = Brushes.Black;
    public List<DrawingVisual> Visuals { get; set; } = new();
    public DrawingVisual CurrentStroke { get; set; }
    public List<Point> CurrentPoints { get; set; } = new();
    public bool IsDrawing { get; set; } = false;
}
