using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DoodlesClient;

public class DrawingHost : FrameworkElement
{
    public int UserThickness { get; set; } = 3;
    public Brush UserBrush { get; set; } = Brushes.Black;

    public List<DrawingVisual> _visuals = new();
    public DrawingVisual _currentStroke;
    public List<Point> _currentPoints = new();
    public bool _isDrawing = false;

    // Called in the background for accessing visuals and displaying
    protected override int VisualChildrenCount => _visuals.Count;
    protected override Visual GetVisualChild(int index) => _visuals[index];

    public DrawingHost()
    {
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseMove += OnMouseMove;
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

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = true;
        _currentPoints.Clear();
        _currentPoints.Add(e.GetPosition(this));

        _currentStroke = new DrawingVisual();
        AddVisual(_currentStroke);

        CaptureMouse(); // keep tracking even if cursor leaves bounds
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;

        _currentPoints.Add(e.GetPosition(this));
        DrawCurrentStroke();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = false;
        _currentStroke = null!;
        _currentPoints.Clear();
        ReleaseMouseCapture();
    }

    private void DrawCurrentStroke()
    {
        if (_currentStroke == null || _currentPoints.Count < 2) return;

        using DrawingContext dc = _currentStroke.RenderOpen();

        Pen pen = new Pen(UserBrush, UserThickness)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(_currentPoints[0], false, false);
            context.PolyLineTo(_currentPoints.Skip(1).ToArray(), true, true);
        }
        geometry.Freeze();

        dc.DrawGeometry(null, pen, geometry);
    }

    public void AddVisual(DrawingVisual visual)
    {
        _visuals.Add(visual);
        AddVisualChild(visual);
        AddLogicalChild(visual);
    }

    public void RemoveVisual(DrawingVisual visual)
    {
        _visuals.Remove(visual);
        RemoveVisualChild(visual);
        RemoveLogicalChild(visual);
    }

    public void Undo()
    {
        if (_visuals.Count == 0) return;
        RemoveVisual(_visuals[^1]);
    }

    public void Clear()
    {
        foreach (var v in _visuals.ToList())
            RemoveVisual(v);
    }
}
