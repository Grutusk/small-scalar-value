using Godot;
using System;
using ScalarValues.Runtime;

public partial class ThingsThatHasScalarValue : Node
{

	[Export] private ScalarValue _scalarValue;
	[Export] private int level = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print($"Scalar value: {_scalarValue.GetScalar(level)}");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("1"))
		{
			level++;
			GD.Print($"Scalar value: {_scalarValue.GetScalar(level)}");
		}
	}
}
