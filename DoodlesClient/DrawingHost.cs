using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DoodlesClient;

public class DrawingHost : FrameworkElement
{
    #region Events & Delegates

    public delegate void MousePoint(Point p);
    public event MousePoint DoodleMouseDownEvent;
    public event MousePoint DoodleMouseMoveEvent;

    public delegate void MouseAction();
    public event MouseAction DoodleMouseUpEvent;
    public event MouseAction DoodleUndoEvent;
    public event MouseAction DoodleClearEvent;

    #endregion Events & Delegates

    #region Properties

    public int UserThickness { get; set; } = 3;
    public Brush UserBrush { get; set; } = Brushes.Black;
    public List<DrawingVisual> Visuals { get; set; } = new();
    public DrawingVisual CurrentStroke { get; set; }
    public List<Point> CurrentPoints { get; set; } = new();
    public bool IsDrawing { get; set; }

    #endregion Properties

    // Called in the background for accessing visuals and displaying
    protected override int VisualChildrenCount => Visuals.Count;
    protected override Visual GetVisualChild(int index) => Visuals[index];

    public DrawingHost()
    {
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseMove += OnMouseMove;
    }

    public void AddVisual(DrawingVisual visual)
    {
        Visuals.Add(visual);
        AddVisualChild(visual);
        AddLogicalChild(visual);
    }

    public void RemoveVisual(DrawingVisual visual)
    {
        Visuals.Remove(visual);
        RemoveVisualChild(visual);
        RemoveLogicalChild(visual);
    }

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

    public void StartStrokeAt(Point p)
    {
        IsDrawing = true;
        CurrentPoints.Clear();
        CurrentPoints.Add(p);

        CurrentStroke = new DrawingVisual();
        AddVisual(CurrentStroke);

        Doodle();
    }

    public void ContinueStrokeAt(Point p)
    {
        if (!IsDrawing) return;
        CurrentPoints.Add(p);
        Doodle();
    }

    public void EndStroke()
    {
        if (!IsDrawing) return;
        IsDrawing = false;
        CurrentStroke = null!;
        CurrentPoints.Clear();
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
        if (Visuals.Count == 0) return;
        RemoveVisual(Visuals[^1]);
        DoodleUndoEvent?.Invoke();
    }

    public void Clear()
    {
        foreach (var v in Visuals.ToList())
            RemoveVisual(v);
        DoodleClearEvent?.Invoke();
    }
}
