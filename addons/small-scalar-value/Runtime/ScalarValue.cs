using Godot;
using Godot.Collections;

namespace ScalarValues.Runtime;

/// <summary>
///     Represents a scalar value that can be retrieved based on a given level or through authored scaling data.
///     This class allows defining scalar values statically, dynamically via a curve, or through level points.
/// </summary>
[GlobalClass]
[Tool]
public partial class ScalarValue : Resource
{
    [ExportGroup("Value")]
    [Export] private float _scalar;
    [Export] private Curve _curve;
    [ExportGroup("Level Table")]
    [Export] private ScalarValueInterpolationMode _levelInterpolationMode = ScalarValueInterpolationMode.Linear;
    [Export] private Array<ScalarValueLevelPoint> _levelPoints;

    public static ScalarValue FromModifier(float value)
    {
        var sc = new ScalarValue();
        sc._scalar = value;
        return sc;
    }

    public float GetScalar(int level = 0)
    {
        return ScalarValueSampling.Sample(_scalar, _curve, _levelInterpolationMode, _levelPoints, level);
    }

}
