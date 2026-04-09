using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GameLibrary.Core;

public class Doodler : FrameworkElement
{
    public delegate void MousePoint(Point p);
    public event MousePoint DoodleMouseDownEvent;
    public event MousePoint DoodleMouseMoveEvent;

    public delegate void MouseAction();
    public event MouseAction DoodleMouseUpEvent;
    public event MouseAction DoodleUndoEvent;
    public event MouseAction DoodleClearEvent;

    public int UserThickness { get; set; } = 3;
    public Brush UserBrush { get; set; } = Brushes.Black;
    public List<DrawingVisual> Doodles { get; set; } = new();
    public DrawingVisual CurrentStroke { get; set; }
    public List<Point> CurrentPoints { get; set; } = new();
    public bool IsDoodling { get; set; }

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
        StartStrokeAt(e.GetPosition(this));
        CaptureMouse(); // keep tracking even if cursor leaves bounds
        DoodleMouseDownEvent?.Invoke(e.GetPosition(this));
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        ContinueStrokeAt(e.GetPosition(this));
        DoodleMouseMoveEvent?.Invoke(e.GetPosition(this));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        EndStroke();
        ReleaseMouseCapture();
        DoodleMouseUpEvent?.Invoke();
    }

    #endregion Events

    #region Methods

    public void StartStrokeAt(Point p)
    {
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
        CurrentPoints.Add(p);
        Doodle();
    }

    public void EndStroke()
    {
        if (!IsDoodling) return;
        IsDoodling = false;
        CurrentStroke = null!;
        CurrentPoints.Clear();
    }

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

        Pen pen = new Pen(UserBrush, UserThickness)
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

    public void Undo()
    {
        if (Doodles.Count == 0) return;
        RemoveDoodle(Doodles[^1]);
        DoodleUndoEvent?.Invoke();
    }

    public void Clear()
    {
        foreach (var v in Doodles.ToList())
            RemoveDoodle(v);
        DoodleClearEvent?.Invoke();
    }

    #endregion Methods
}
