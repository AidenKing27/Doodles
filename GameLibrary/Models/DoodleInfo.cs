using GameLibrary.Enums;
using System.Windows;
using System.Windows.Media;

namespace GameLibrary.Models;

public class DoodleInfo
{
    public DoodleType DoodleType { get; set; }
    public bool IsErasing { get; set; }
    public int? UserThickness { get; set; }
    public Color? UserColour { get; set; }
    public Point? Point { get; set; }

    public static DoodleInfo CreateDoodleInfo(DoodleType type, int? thickness, Color? colour, Point? p = null, bool? isErasing = null)
    {
        return new()
        {
            DoodleType = type,
            Point = p,
            UserThickness = thickness,
            UserColour = colour
        };
    }

    public static DoodleInfo CreateDoodleInfoErasing(DoodleType type, bool isErasing)
    {
        return new()
        {
            DoodleType = type,
            IsErasing = isErasing
        };
    }

    public static DoodleInfo CreateDoodleInfoThickness(DoodleType type, int thickness)
    {
        return new()
        {
            DoodleType = type,
            UserThickness = thickness
        };
    }
}
