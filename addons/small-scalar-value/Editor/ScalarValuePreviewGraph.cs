using ScalarValues.Collections;
using ScalarValues.Runtime;
#if TOOLS
using System.Collections.Generic;
using Godot;

namespace ScalarValues.Editor;

[GlobalClass]
[Tool]
public partial class ScalarValuePreviewGraph : Control
{
    private Curve _curve;
    private ScalarValueInterpolationMode _interpolationMode;
    private List<Vector2> _points = new();

    public ScalarValuePreviewGraph()
    {
        CustomMinimumSize = new Vector2(0f, 180f);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void UpdatePreview(ScalarValueInterpolationMode interpolationMode,
        IEnumerable<ScalarValueLevelPoint> authoredPoints, Curve curve, float fallbackScalar)
    {
        _interpolationMode = interpolationMode;
        _curve = curve;
        _points = authoredPoints?
            .Where(point => point != null)
            .Select(point => new Vector2(point.Level, point.Value))
            .OrderBy(point => point.X)
            .ToList() ?? new List<Vector2>();

        if (_points.Count == 0 && fallbackScalar != 0f)
            _points.Add(new Vector2(1f, fallbackScalar));

        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();

        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, new Color(0.08f, 0.1f, 0.13f, 0.95f), true);
        DrawRect(rect, new Color(0.24f, 0.28f, 0.34f, 1f), false, 1.2f);

        var plotRect = rect.GrowIndividual(-14f, -14f, -14f, -14f);
        if (plotRect.Size.X <= 1f || plotRect.Size.Y <= 1f)
            return;

        DrawGrid(plotRect);

        if (_points.Count > 1)
        {
            DrawLevelPointPreview(plotRect);
            return;
        }

        if (_curve != null && _curve.PointCount > 0)
        {
            DrawCurvePreview(plotRect);
            return;
        }

        if (_points.Count == 1)
        {
            DrawFlatPreview(plotRect, _points[0].Y);
            return;
        }

        DrawString(GetThemeDefaultFont(), plotRect.Position + new Vector2(0f, 16f),
            "Add level points to preview the value graph.", modulate: new Color(0.77f, 0.81f, 0.88f, 0.92f));
    }

    private void DrawGrid(Rect2 plotRect)
    {
        var verticalColor = new Color(1f, 1f, 1f, 0.06f);
        var horizontalColor = new Color(1f, 1f, 1f, 0.08f);

        for (var i = 1; i < 4; i++)
        {
            var t = i / 4f;
            var x = Mathf.Lerp(plotRect.Position.X, plotRect.End.X, t);
            var y = Mathf.Lerp(plotRect.Position.Y, plotRect.End.Y, t);
            DrawLine(new Vector2(x, plotRect.Position.Y), new Vector2(x, plotRect.End.Y), verticalColor, 1f);
            DrawLine(new Vector2(plotRect.Position.X, y), new Vector2(plotRect.End.X, y), horizontalColor, 1f);
        }
    }

    private void DrawLevelPointPreview(Rect2 plotRect)
    {
        GetBounds(_points, out var minX, out var maxX, out var minY, out var maxY);
        ExpandFlatBounds(ref minX, ref maxX);
        ExpandFlatBounds(ref minY, ref maxY);

        var lineColor = new Color(0.26f, 0.8f, 0.63f, 1f);
        var pointFill = new Color(0.93f, 0.98f, 0.99f, 1f);
        var pointOutline = new Color(0.12f, 0.16f, 0.2f, 1f);

        for (var i = 0; i < _points.Count - 1; i++)
        {
            var current = MapToPlot(_points[i], plotRect, minX, maxX, minY, maxY);
            var next = MapToPlot(_points[i + 1], plotRect, minX, maxX, minY, maxY);

            if (_interpolationMode == ScalarValueInterpolationMode.Step)
            {
                var stepCorner = new Vector2(next.X, current.Y);
                DrawLine(current, stepCorner, lineColor, 2f);
                DrawLine(stepCorner, next, lineColor, 2f);
            }
            else if (_interpolationMode is ScalarValueInterpolationMode.EaseInOut or ScalarValueInterpolationMode.Cubic)
            {
                // Non-linear modes are previewed by sampling the runtime interpolator, so the graph matches gameplay.
                DrawSampledLevelPointCurve(plotRect, minX, maxX, minY, maxY, lineColor);
                break;
            }
            else
            {
                DrawLine(current, next, lineColor, 2f);
            }
        }

        foreach (var point in _points)
        {
            var mapped = MapToPlot(point, plotRect, minX, maxX, minY, maxY);
            DrawCircle(mapped, 4.5f, pointFill);
            DrawArc(mapped, 4.5f, 0f, Mathf.Tau, 16, pointOutline, 1.25f);
        }
    }

    private void DrawSampledLevelPointCurve(Rect2 plotRect, float minX, float maxX, float minY, float maxY,
        Color lineColor)
    {
        if (_points.Count < 2)
            return;

        const int sampleCount = 64;
        var sampleMinX = _points[0].X;
        var sampleMaxX = _points[_points.Count - 1].X;
        Vector2? previous = null;

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)(sampleCount - 1);
            var x = Mathf.Lerp(sampleMinX, sampleMaxX, t);
            if (!ScalarValueSampling.TrySamplePreview(_interpolationMode, _points, x, out var sampledValue))
                continue;

            var mapped = MapToPlot(new Vector2(x, sampledValue), plotRect, minX, maxX, minY, maxY);
            if (previous.HasValue)
                DrawLine(previous.Value, mapped, lineColor, 2f);

            previous = mapped;
        }
    }

    private void DrawCurvePreview(Rect2 plotRect)
    {
        var curvePoints = new List<Vector2>();
        for (var i = 0; i < _curve.PointCount; i++)
            curvePoints.Add(_curve.GetPointPosition(i));

        GetBounds(curvePoints, out var minX, out var maxX, out var minY, out var maxY);
        ExpandFlatBounds(ref minX, ref maxX);
        ExpandFlatBounds(ref minY, ref maxY);

        var lineColor = new Color(0.83f, 0.65f, 0.25f, 1f);
        const int sampleCount = 48;
        Vector2? previous = null;
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)(sampleCount - 1);
            var x = Mathf.Lerp(minX, maxX, t);
            var sampled = new Vector2(x, _curve.Sample(x));
            var mapped = MapToPlot(sampled, plotRect, minX, maxX, minY, maxY);
            if (previous.HasValue)
                DrawLine(previous.Value, mapped, lineColor, 2f);

            previous = mapped;
        }
    }

    private void DrawFlatPreview(Rect2 plotRect, float value)
    {
        var start = new Vector2(plotRect.Position.X, plotRect.GetCenter().Y);
        var end = new Vector2(plotRect.End.X, plotRect.GetCenter().Y);
        DrawLine(start, end, new Color(0.6f, 0.72f, 0.95f, 1f), 2f);

        DrawString(GetThemeDefaultFont(), plotRect.Position + new Vector2(0f, 16f),
            $"Flat value: {value:0.##}", modulate: new Color(0.77f, 0.81f, 0.88f, 0.92f));
    }

    private static void GetBounds(IReadOnlyList<Vector2> points, out float minX, out float maxX, out float minY,
        out float maxY)
    {
        minX = points[0].X;
        maxX = points[0].X;
        minY = points[0].Y;
        maxY = points[0].Y;

        for (var i = 1; i < points.Count; i++)
        {
            var point = points[i];
            minX = Mathf.Min(minX, point.X);
            maxX = Mathf.Max(maxX, point.X);
            minY = Mathf.Min(minY, point.Y);
            maxY = Mathf.Max(maxY, point.Y);
        }

        var xPadding = Mathf.Max(1f, (maxX - minX) * 0.08f);
        var yPadding = Mathf.Max(1f, (maxY - minY) * 0.12f);
        minX -= xPadding;
        maxX += xPadding;
        minY -= yPadding;
        maxY += yPadding;
    }

    private static void ExpandFlatBounds(ref float min, ref float max)
    {
        if (!Mathf.IsEqualApprox(min, max))
            return;

        min -= 1f;
        max += 1f;
    }

    private static Vector2 MapToPlot(Vector2 point, Rect2 plotRect, float minX, float maxX, float minY, float maxY)
    {
        var xT = Mathf.InverseLerp(minX, maxX, point.X);
        var yT = Mathf.InverseLerp(minY, maxY, point.Y);
        return new Vector2(
            Mathf.Lerp(plotRect.Position.X, plotRect.End.X, xT),
            Mathf.Lerp(plotRect.End.Y, plotRect.Position.Y, yT));
    }
}
#endif
