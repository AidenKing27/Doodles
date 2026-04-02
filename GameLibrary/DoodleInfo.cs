using System.Windows;
using System.Windows.Media;

namespace GameLibrary;

public class DoodleInfo
{
    public DoodleType DoodleType { get; set; }
    public Point? Point { get; set; }
    public int? UserThickness { get; set; }
    public Brush? UserBrush { get; set; }
}
