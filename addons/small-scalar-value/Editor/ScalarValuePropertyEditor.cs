using ScalarValues.Collections;
using ScalarValues.Runtime;
#if TOOLS
using System;
using Godot;
using Godot.Collections;

namespace ScalarValues.Editor;

[GlobalClass]
[Tool]
public partial class ScalarValuePropertyEditor : EditorProperty
{
    private const string ScalarPropertyName = "_scalar";
    private const string CurvePropertyName = "_curve";
    private const string LevelInterpolationModePropertyName = "_levelInterpolationMode";
    private const string LevelPointsPropertyName = "_levelPoints";
    private const float LevelColumnWidth = 92f;
    private const float ActionColumnWidth = 88f;
    private const double ScalarStep = 0.01;

    private readonly Button _addPointButton;
    private readonly Button _clearPointsButton;
    private readonly EditorResourcePicker _curvePicker;
    private readonly OptionButton _interpolationPicker;
    private readonly ScalarValuePreviewGraph _previewGraph;
    private readonly VBoxContainer _rowsContainer;
    private readonly ScrollContainer _rowsScroll;
    private readonly SpinBox _scalarSpinBox;
    private readonly Label _summaryLabel;
    private Array<ScalarValueLevelPoint> _currentLevelPoints = new();
    private bool _hasCurrentLevelPoints;
    private bool _isUpdating;

    public ScalarValuePropertyEditor()
    {
        // Render as a bottom editor so Godot does not reserve the usual left-side property label column.
        DrawLabel = false;

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);
        SetBottomEditor(root);

        var introLabel = new Label
        {
            Text = "Author breakpoints directly instead of using the Godot curve editor.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(introLabel);

        _scalarSpinBox = CreateSpinBox(step: ScalarStep, rounded: false);
        _scalarSpinBox.MinValue = -999999f;
        _scalarSpinBox.MaxValue = 999999f;
        _scalarSpinBox.ValueChanged += value =>
        {
            if (_isUpdating)
                return;

            EmitChanged(ScalarPropertyName, (float)value);
            RefreshLevelPointDisplay(_currentLevelPoints, rebuildRows: false);
        };
        root.AddChild(CreateFieldRow("Value", _scalarSpinBox));

        _curvePicker = new EditorResourcePicker
        {
            BaseType = nameof(Curve),
            Editable = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _curvePicker.ResourceChanged += resource =>
        {
            if (_isUpdating)
                return;

            EmitChanged(CurvePropertyName, resource);
            RefreshLevelPointDisplay(_currentLevelPoints, rebuildRows: false);
        };
        root.AddChild(CreateFieldRow("Curve", _curvePicker));

        _interpolationPicker = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        PopulateInterpolationModes();
        _interpolationPicker.ItemSelected += index =>
        {
            if (_isUpdating)
                return;

            EmitChanged(LevelInterpolationModePropertyName, _interpolationPicker.GetItemId((int)index));
            RefreshLevelPointDisplay(_currentLevelPoints, rebuildRows: false);
        };
        root.AddChild(CreateFieldRow("Level Table Mode", _interpolationPicker));

        var toolbar = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _addPointButton = new Button
        {
            Text = "Add Point",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _addPointButton.Pressed += OnAddPointPressed;
        toolbar.AddChild(_addPointButton);

        _clearPointsButton = new Button
        {
            Text = "Clear Points",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _clearPointsButton.Pressed += OnClearPointsPressed;
        toolbar.AddChild(_clearPointsButton);
        root.AddChild(toolbar);

        _summaryLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(_summaryLabel);

        var rowsPanel = new PanelContainer();
        rowsPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.AddChild(rowsPanel);

        var rowsMargin = new MarginContainer();
        rowsMargin.AddThemeConstantOverride("margin_left", 10);
        rowsMargin.AddThemeConstantOverride("margin_top", 8);
        rowsMargin.AddThemeConstantOverride("margin_right", 10);
        rowsMargin.AddThemeConstantOverride("margin_bottom", 8);
        rowsPanel.AddChild(rowsMargin);

        var rowsVBox = new VBoxContainer();
        rowsVBox.AddThemeConstantOverride("separation", 6);
        rowsMargin.AddChild(rowsVBox);

        rowsVBox.AddChild(CreateHeaderRow());

        _rowsScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0f, 170f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _rowsScroll.Resized += OnRowsScrollResized;
        rowsVBox.AddChild(_rowsScroll);

        _rowsContainer = new VBoxContainer();
        _rowsContainer.AddThemeConstantOverride("separation", 4);
        _rowsContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _rowsScroll.AddChild(_rowsContainer);

        _previewGraph = new ScalarValuePreviewGraph
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        root.AddChild(_previewGraph);
    }

    public override void _UpdateProperty()
    {
        _isUpdating = true;

        try
        {
            var editedObject = GetEditedObject();
            if (editedObject == null || !IsInstanceValid(editedObject))
            {
                _currentLevelPoints = new Array<ScalarValueLevelPoint>();
                _hasCurrentLevelPoints = false;
                return;
            }

            var scalar = editedObject.Get(ScalarPropertyName).As<float>();
            var curve = editedObject.Get(CurvePropertyName).As<Curve>();
            var interpolationMode =
                (ScalarValueInterpolationMode)editedObject.Get(LevelInterpolationModePropertyName).As<int>();
            var levelPoints = editedObject.Get(LevelPointsPropertyName).As<Array<ScalarValueLevelPoint>>() ??
                              new Array<ScalarValueLevelPoint>();
            _currentLevelPoints = CloneLevelPoints(levelPoints);
            _hasCurrentLevelPoints = true;

            _scalarSpinBox.Value = scalar;
            _curvePicker.EditedResource = curve;
            SelectInterpolationMode(interpolationMode);
            RefreshLevelPointDisplay(_currentLevelPoints, rebuildRows: true);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnAddPointPressed()
    {
        var levelPoints = ReadLevelPoints();
        var lastPoint = levelPoints.Count == 0 ? null : levelPoints[levelPoints.Count - 1];
        var nextLevel = lastPoint?.Level + 1 ?? 1;
        var nextValue = levelPoints.Count == 0 ? (float)_scalarSpinBox.Value : lastPoint?.Value ?? 0f;
        levelPoints.Add(ScalarValueLevelPoint.Create(nextLevel, nextValue));
        EmitLevelPointsChanged(levelPoints);
    }

    private void OnClearPointsPressed()
    {
        EmitLevelPointsChanged(new Array<ScalarValueLevelPoint>());
    }

    private void RebuildRows(Array<ScalarValueLevelPoint> levelPoints)
    {
        foreach (var child in _rowsContainer.GetChildren())
        {
            // Rows can be rebuilt from a child control signal, so defer deletion until the signal unwinds.
            child.QueueFree();
            _rowsContainer.RemoveChild(child);
        }

        if (levelPoints.Count == 0)
        {
            _rowsContainer.AddChild(new Label
            {
                Text = "No level points authored. Value or curve will be used.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        for (var index = 0; index < levelPoints.Count; index++)
        {
            var point = levelPoints[index];
            if (point == null)
                continue;

            _rowsContainer.AddChild(CreatePointRow(index, point));
        }
    }

    private Control CreatePointRow(int index, ScalarValueLevelPoint point)
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        var levelSpin = CreateSpinBox(step: 1, rounded: true);
        levelSpin.MinValue = 1;
        levelSpin.MaxValue = 9999;
        levelSpin.Value = point.Level;
        levelSpin.CustomMinimumSize = new Vector2(LevelColumnWidth, 0f);
        levelSpin.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        levelSpin.ValueChanged += value =>
        {
            if (_isUpdating)
                return;

            var points = ReadLevelPoints();
            if (index < 0 || index >= points.Count)
                return;

            var existing = points[index];
            points[index] = ScalarValueLevelPoint.Create((int)Mathf.Round(value), existing?.Value ?? 0f);
            EmitLevelPointsChanged(points, rebuildRows: false);
        };
        row.AddChild(levelSpin);

        var valueSpin = CreateSpinBox(step: ScalarStep, rounded: false);
        valueSpin.MinValue = -999999;
        valueSpin.MaxValue = 999999;
        valueSpin.Value = point.Value;
        valueSpin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        valueSpin.ValueChanged += value =>
        {
            if (_isUpdating)
                return;

            var points = ReadLevelPoints();
            if (index < 0 || index >= points.Count)
                return;

            var existing = points[index];
            points[index] = ScalarValueLevelPoint.Create(existing?.Level ?? 1, (float)value);
            EmitLevelPointsChanged(points, rebuildRows: false);
        };
        row.AddChild(valueSpin);

        var removeButton = new Button
        {
            Text = "Remove",
            CustomMinimumSize = new Vector2(ActionColumnWidth, 0f)
        };
        removeButton.Pressed += () =>
        {
            if (_isUpdating)
                return;

            var points = ReadLevelPoints();
            if (index < 0 || index >= points.Count)
                return;

            points.RemoveAt(index);
            EmitLevelPointsChanged(points);
        };
        row.AddChild(removeButton);

        return row;
    }

    private void OnRowsScrollResized()
    {
        UpdateRowsWidthDeferred();
    }

    private void PopulateInterpolationModes()
    {
        foreach (ScalarValueInterpolationMode mode in Enum.GetValues(typeof(ScalarValueInterpolationMode)))
            _interpolationPicker.AddItem(FormatInterpolationLabel(mode), (int)mode);
    }

    private void SelectInterpolationMode(ScalarValueInterpolationMode interpolationMode)
    {
        for (var i = 0; i < _interpolationPicker.ItemCount; i++)
        {
            if (_interpolationPicker.GetItemId(i) != (int)interpolationMode)
                continue;

            _interpolationPicker.Select(i);
            return;
        }

        _interpolationPicker.Select(0);
    }

    private static string FormatInterpolationLabel(ScalarValueInterpolationMode mode)
    {
        return mode switch
        {
            ScalarValueInterpolationMode.EaseInOut => "Ease In Out",
            _ => mode.ToString()
        };
    }

    private void UpdateRowsWidthDeferred()
    {
        if (_rowsScroll == null || !IsInstanceValid(_rowsScroll) ||
            _rowsContainer == null || !IsInstanceValid(_rowsContainer))
            return;

        // Keep the scroll content width locked to the viewport so the header and rows stay aligned.
        var width = Mathf.Max(0f, _rowsScroll.Size.X - 12f);
        _rowsContainer.CustomMinimumSize = new Vector2(width, 0f);
    }

    private void EmitLevelPointsChanged(Array<ScalarValueLevelPoint> levelPoints, bool rebuildRows = true)
    {
        _currentLevelPoints = CloneLevelPoints(levelPoints);
        _hasCurrentLevelPoints = true;
        EmitChanged(LevelPointsPropertyName, _currentLevelPoints);
        RefreshLevelPointDisplay(_currentLevelPoints, rebuildRows);
    }

    private Array<ScalarValueLevelPoint> ReadLevelPoints()
    {
        var editedObject = GetEditedObject();
        if (editedObject == null || !IsInstanceValid(editedObject))
            return new Array<ScalarValueLevelPoint>();

        if (_hasCurrentLevelPoints)
            return CloneLevelPoints(_currentLevelPoints);

        var currentPoints = editedObject.Get(LevelPointsPropertyName).As<Array<ScalarValueLevelPoint>>();
        return CloneLevelPoints(currentPoints);
    }

    private void RefreshLevelPointDisplay(Array<ScalarValueLevelPoint> levelPoints, bool rebuildRows)
    {
        var wasUpdating = _isUpdating;
        _isUpdating = true;

        try
        {
            var interpolationMode = GetSelectedInterpolationMode();
            var curve = _curvePicker.EditedResource as Curve;
            var scalar = (float)_scalarSpinBox.Value;
            if (rebuildRows)
                RebuildRows(levelPoints);
            UpdateSummary(levelPoints, interpolationMode, curve);
            _previewGraph.UpdatePreview(interpolationMode, levelPoints, curve, scalar);
            CallDeferred(nameof(UpdateRowsWidthDeferred));
        }
        finally
        {
            _isUpdating = wasUpdating;
        }
    }

    private ScalarValueInterpolationMode GetSelectedInterpolationMode()
    {
        var selectedIndex = _interpolationPicker.Selected;
        if (selectedIndex >= 0)
            return (ScalarValueInterpolationMode)_interpolationPicker.GetItemId(selectedIndex);

        return ScalarValueInterpolationMode.Linear;
    }

    private static Array<ScalarValueLevelPoint> CloneLevelPoints(Array<ScalarValueLevelPoint> currentPoints)
    {
        var clone = new Array<ScalarValueLevelPoint>();
        if (currentPoints == null)
            return clone;

        // Edit against copies so typing in the inspector does not mutate the live array before EmitChanged.
        foreach (var point in currentPoints)
        {
            clone.Add(point == null ? null : ScalarValueLevelPoint.Create(point.Level, point.Value));
        }

        return clone;
    }

    private void UpdateSummary(Array<ScalarValueLevelPoint> levelPoints, ScalarValueInterpolationMode interpolationMode,
        Curve curve)
    {
        _clearPointsButton.Disabled = levelPoints.Count == 0;

        if (levelPoints.Count > 0)
        {
            ScalarValueLevelPoint first = null;
            ScalarValueLevelPoint last = null;
            foreach (var point in levelPoints)
            {
                if (point == null)
                    continue;

                if (first == null || point.Level < first.Level)
                    first = point;

                if (last == null || point.Level > last.Level)
                    last = point;
            }

            _summaryLabel.Text =
                $"Authoring {levelPoints.Count} point(s), levels {first?.Level ?? 1}-{last?.Level ?? 1}, mode: {interpolationMode}. Level Table overrides the base value and curve.";
            return;
        }

        _summaryLabel.Text = curve == null
            ? "No Level Table points. The base value is used."
            : "No Level Table points. The curve is active.";
    }

    private static Control CreateHeaderRow()
    {
        var header = new HBoxContainer();

        var levelLabel = new Label
        {
            Text = "Level",
            CustomMinimumSize = new Vector2(LevelColumnWidth, 0f),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin
        };
        header.AddChild(levelLabel);

        var valueLabel = new Label
        {
            Text = "Value",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        header.AddChild(valueLabel);

        header.AddChild(new Label
        {
            Text = string.Empty,
            CustomMinimumSize = new Vector2(ActionColumnWidth, 0f)
        });
        return header;
    }

    private static HBoxContainer CreateFieldRow(string labelText, Control field)
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        var label = new Label
        {
            Text = labelText,
            CustomMinimumSize = new Vector2(110f, 0f),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin
        };
        row.AddChild(label);

        field.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(field);
        return row;
    }

    private static SpinBox CreateSpinBox(double step, bool rounded)
    {
        return new SpinBox
        {
            Step = step,
            Rounded = rounded,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
    }
}
#endif
