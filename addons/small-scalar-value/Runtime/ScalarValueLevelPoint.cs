using ScalarValues.Collections;
using System;
using Godot;

namespace ScalarValues.Runtime;

[GlobalClass]
[Tool]
public partial class ScalarValueLevelPoint : Resource
{
    [Export(PropertyHint.Range, "1,999,1,or_greater")] private int _level = 1;
    [Export] private float _value;

    public int Level => Math.Max(1, _level);
    public float Value => _value;

    public static ScalarValueLevelPoint Create(int level, float value)
    {
        return new ScalarValueLevelPoint
        {
            _level = Math.Max(1, level),
            _value = value
        };
    }
}
