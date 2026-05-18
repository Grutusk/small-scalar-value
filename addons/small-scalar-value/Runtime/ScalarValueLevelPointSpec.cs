using ScalarValues.Collections;
using System;

namespace ScalarValues.Runtime;

public readonly struct ScalarValueLevelPointSpec
{
    public int Level { get; init; }
    public float Value { get; init; }

    public int GetNormalizedLevel()
    {
        return Math.Max(1, Level);
    }
}
