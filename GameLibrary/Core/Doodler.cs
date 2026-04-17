using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GameLibrary.Core;

public class Doodler : FrameworkElement
{
    public delegate void PointHandler(Point p);
    public event PointHandler DoodleMouseDownEvent;
    public event PointHandler DoodleMouseMoveEvent;

    public delegate void ActionHandler();
    public event ActionHandler DoodleMouseUpEvent;
    public event ActionHandler DoodleUndoEvent;
    public event ActionHandler DoodleClearEvent;

    //public delegate void PencilEraseHandler(bool isErasing);
    //public event PencilEraseHandler DoodlePencilEraseEvent;

    //public delegate void ThicknessHandler(int thickness);
    //public event ThicknessHandler DoodleThicknessEvent;

    public bool IsErasing { get; set; }
    public int UserThickness { get; set; } = 12;
    public Color UserColour { get; set; } = (Color)ColorConverter.ConvertFromString(Palette["Black"]);
    public List<DrawingVisual> Doodles { get; set; } = new();
    public DrawingVisual CurrentStroke { get; set; }
    public List<Point> CurrentPoints { get; set; } = new();
    public bool IsDoodling { get; set; }

    public static readonly Dictionary<string, string> Palette = new()
    {
        ["Black"] = "#000000",
        ["Grey"] = "#7F7F7F",
        ["Dark Red"] = "#880015",
        ["Red"] = "#ED1C24",
        ["Orange"] = "#FF7F27",
        ["Yellow"] = "#FFF200",
        ["Green"] = "#22B14C",
        ["Turquoise"] = "#00A2E8",
        ["Indigo"] = "#3F48CC",
        ["Purple"] = "#A349A4",
        ["White"] = "#FFFFFF",
        ["Light Grey"] = "#C3C3C3",
        ["Brown"] = "#B97A57",
        ["Rose"] = "#FFAEC9",
        ["Gold"] = "#FFC90E",
        ["Light Yellow"] = "#EFE4B0",
        ["Lime"] = "#B5E61D",
        ["Light Turquoise"] = "#99D9EA",
        ["Cyan"] = "#7092BE",
        ["Lavender"] = "#C8BFE7"
    };

    public Doodler()
    {
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseMove += OnMouseMove;
    }

    #region Hidden Functionality

    // Called in the background for accessing visuals and displaying
    protected override int VisualChildrenCount => Doodles.Count;
    protected override Visual GetVisualChild(int index) => Doodles[index];

    // Tells WPF to allow the entire DrawingHost surface to be clickable
    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParams)
    {
        return new PointHitTestResult(this, hitTestParams.HitPoint);
    }

    // Ensures lines dont go passed the border
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        Clip = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
    }

    #endregion Hidden Functionality  

    #region Events

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        IsDoodling = true;
        StartStrokeAt(e.GetPosition(this));
        CaptureMouse(); // keep tracking even if cursor leaves bounds
        DoodleMouseDownEvent?.Invoke(e.GetPosition(this));
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!IsDoodling) return;

        ContinueStrokeAt(e.GetPosition(this));
        DoodleMouseMoveEvent?.Invoke(e.GetPosition(this));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!IsDoodling) return;

        EndStroke();
        ReleaseMouseCapture();
        DoodleMouseUpEvent?.Invoke();
    }

    public void RequestUndo()
    {
        PerformUndo();
        DoodleUndoEvent?.Invoke();
    }

    public void RequestClear()
    {
        PerformClear();
        DoodleClearEvent?.Invoke();
    }

    public void RequestPencilErase(bool isErasing)
    {
        PerformPencilErase(isErasing);
        //DoodlePencilEraseEvent?.Invoke(isErasing);
    }

    public void RequestSetThickness(int thickness)
    {
        PerformSetThickness(thickness);
        //DoodleThicknessEvent?.Invoke(thickness);
    }

    #endregion Events

    #region Public Accessors

    public void StartStrokeAt(Point p)
    {
        if (IsErasing) 
            UserColour = (Color)ColorConverter.ConvertFromString(Palette["White"]);
        IsDoodling = true;
        CurrentPoints.Clear();
        CurrentPoints.Add(p);

        CurrentStroke = new DrawingVisual();
        AddDoodle(CurrentStroke);

        Doodle();
    }

    public void ContinueStrokeAt(Point p)
    {
        if (!IsDoodling) return;

        if (IsErasing) 
            UserColour = (Color)ColorConverter.ConvertFromString(Palette["White"]);
        CurrentPoints.Add(p);
        Doodle();
    }

    public void EndStroke()
    {
        if (!IsDoodling) return;

        if (IsErasing) 
            UserColour = (Color)ColorConverter.ConvertFromString(Palette["White"]);
        IsDoodling = false;
        CurrentStroke = null!;
        CurrentPoints.Clear();
    }

    public void PerformUndo()
    {
        if (Doodles.Count == 0) return;
        RemoveDoodle(Doodles[^1]);
    }

    public void PerformClear()
    {
        foreach (var v in Doodles.ToList())
            RemoveDoodle(v);
    }

    public void PerformPencilErase(bool isErasing)
    {
        IsErasing = isErasing;
    }

    public void PerformSetThickness(int thickness)
    {
        UserThickness = thickness;
    }

    #endregion Public Accessors

    #region Methods

    public void AddDoodle(DrawingVisual visual)
    {
        Doodles.Add(visual);
        AddVisualChild(visual);
        AddLogicalChild(visual);
    }

    public void RemoveDoodle(DrawingVisual visual)
    {
        Doodles.Remove(visual);
        RemoveVisualChild(visual);
        RemoveLogicalChild(visual);
    }

    public void Doodle()
    {
        if (CurrentStroke == null || CurrentPoints.Count < 2) return;

        using DrawingContext dc = CurrentStroke.RenderOpen();

        Brush brush = new SolidColorBrush(UserColour);
        Pen pen = new Pen(brush, UserThickness)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(CurrentPoints[0], false, false);
            context.PolyLineTo(CurrentPoints.Skip(1).ToArray(), true, true);
        }

        geometry.Freeze();

        dc.DrawGeometry(null, pen, geometry);
    }

    

    

    #endregion Methods
}
