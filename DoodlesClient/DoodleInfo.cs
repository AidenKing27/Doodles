using System.Windows;
using System.Windows.Media;

namespace DoodlesClient;

public class DoodleInfo
{
    public int UserThickness { get; set; } = 3;
    public Brush UserBrush { get; set; } = Brushes.Black;
    public List<DrawingVisual> Doodles { get; set; } = new();
    public DrawingVisual CurrentStroke { get; set; }
    public List<Point> CurrentPoints { get; set; } = new();
    public bool IsDoodling { get; set; } = false;
}
