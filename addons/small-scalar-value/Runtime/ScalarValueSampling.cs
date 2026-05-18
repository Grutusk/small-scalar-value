using ScalarValues.Collections;
using System;
using System.Collections.Generic;
using Godot;

namespace ScalarValues.Runtime;

internal static class ScalarValueSampling
{
    public static float Sample(float scalar, Curve curve, ScalarValueInterpolationMode interpolationMode,
        IEnumerable<ScalarValueLevelPoint> levelPoints, int level)
    {
        if (level == 0)
            return scalar;

        if (TrySample(level, interpolationMode, levelPoints, static point => point != null,
                static point => point.Level,
                static point => point.Value,
                out var sampledValue))
            return sampledValue;

        return curve?.Sample(level) ?? scalar;
    }

    public static float Sample(float scalar, Curve curve, ScalarValueInterpolationMode interpolationMode,
        IReadOnlyList<ScalarValueLevelPointSpec> levelPoints, int level)
    {
        if (level == 0)
            return scalar;

        if (TrySample(level, interpolationMode, levelPoints, static _ => true,
                static point => point.GetNormalizedLevel(),
                static point => point.Value,
                out var sampledValue))
            return sampledValue;

        return curve?.Sample(level) ?? scalar;
    }

    internal static bool TrySamplePreview(ScalarValueInterpolationMode interpolationMode, IEnumerable<Vector2> levelPoints,
        float level, out float sampledValue)
    {
        return TrySample(level, interpolationMode, levelPoints, static _ => true,
            static point => point.X,
            static point => point.Y,
            out sampledValue);
    }

    private static bool TrySample<T>(float level, ScalarValueInterpolationMode interpolationMode, IEnumerable<T> levelPoints,
        Func<T, bool> isValid,
        Func<T, float> getLevel,
        Func<T, float> getValue,
        out float sampledValue)
    {
        sampledValue = 0f;
        if (levelPoints == null)
            return false;

        var points = new List<SamplePoint>();

        foreach (var point in levelPoints)
        {
            if (!isValid(point))
                continue;

            points.Add(new SamplePoint(Mathf.Max(1f, getLevel(point)), getValue(point)));
        }

        if (points.Count == 0)
            return false;

        points.Sort(static (a, b) => a.Level.CompareTo(b.Level));
        level = Mathf.Max(1f, level);

        // Treat authored tables as clamped ranges instead of extrapolating beyond the first/last key.
        if (level <= points[0].Level)
        {
            sampledValue = points[0].Value;
            return true;
        }

        var lastIndex = points.Count - 1;
        if (level >= points[lastIndex].Level)
        {
            sampledValue = points[lastIndex].Value;
            return true;
        }

        var upperIndex = 1;
        while (upperIndex < points.Count && points[upperIndex].Level < level)
            upperIndex++;

        if (upperIndex >= points.Count)
        {
            sampledValue = points[lastIndex].Value;
            return true;
        }

        if (Mathf.IsEqualApprox(points[upperIndex].Level, level))
        {
            sampledValue = points[upperIndex].Value;
            return true;
        }

        // Once the surrounding segment is known, all interpolation modes resolve inside that one interval.
        var lowerIndex = Math.Max(0, upperIndex - 1);
        sampledValue = Interpolate(points, lowerIndex, upperIndex, level, interpolationMode);
        return true;
    }

    private static float Interpolate(IReadOnlyList<SamplePoint> points, int lowerIndex, int upperIndex, float level,
        ScalarValueInterpolationMode interpolationMode)
    {
        var lower = points[lowerIndex];
        var upper = points[upperIndex];
        var distance = upper.Level - lower.Level;
        if (distance <= Mathf.Epsilon)
            return lower.Value;

        var t = Mathf.Clamp((level - lower.Level) / distance, 0f, 1f);
        return interpolationMode switch
        {
            ScalarValueInterpolationMode.Step => lower.Value,
            ScalarValueInterpolationMode.EaseInOut =>
                Mathf.Lerp(lower.Value, upper.Value, Mathf.SmoothStep(0f, 1f, t)),
            ScalarValueInterpolationMode.Cubic => InterpolateCubic(points, lowerIndex, upperIndex, t),
            _ => Mathf.Lerp(lower.Value, upper.Value, t)
        };
    }

    private static float InterpolateCubic(IReadOnlyList<SamplePoint> points, int lowerIndex, int upperIndex, float t)
    {
        var lower = points[lowerIndex];
        var upper = points[upperIndex];
        var segmentWidth = upper.Level - lower.Level;
        if (segmentWidth <= Mathf.Epsilon)
            return lower.Value;

        var slopeLower = EstimateSlope(points, lowerIndex);
        var slopeUpper = EstimateSlope(points, upperIndex);

        var t2 = t * t;
        var t3 = t2 * t;

        var h00 = 2f * t3 - 3f * t2 + 1f;
        var h10 = t3 - 2f * t2 + t;
        var h01 = -2f * t3 + 3f * t2;
        var h11 = t3 - t2;

        var value =
            h00 * lower.Value +
            h10 * segmentWidth * slopeLower +
            h01 * upper.Value +
            h11 * segmentWidth * slopeUpper;

        // Clamp the Hermite result to the segment endpoints so authored stat tables do not overshoot.
        var min = Mathf.Min(lower.Value, upper.Value);
        var max = Mathf.Max(lower.Value, upper.Value);
        return Mathf.Clamp(value, min, max);
    }

    private static float EstimateSlope(IReadOnlyList<SamplePoint> points, int index)
    {
        if (points.Count < 2)
            return 0f;

        if (index <= 0)
            return CalculateSlope(points[0], points[1]);

        if (index >= points.Count - 1)
            return CalculateSlope(points[points.Count - 2], points[points.Count - 1]);

        return CalculateSlope(points[index - 1], points[index + 1]);
    }

    private static float CalculateSlope(SamplePoint from, SamplePoint to)
    {
        var distance = to.Level - from.Level;
        if (distance <= Mathf.Epsilon)
            return 0f;

        return (to.Value - from.Value) / distance;
    }

}
