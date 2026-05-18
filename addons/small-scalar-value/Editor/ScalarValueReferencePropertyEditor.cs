#if TOOLS
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;
using Godot.Collections;
using ScalarValues.Runtime;

namespace ScalarValues.Editor;

[GlobalClass]
[Tool]
public partial class ScalarValueReferencePropertyEditor : EditorProperty, ISerializationListener
{
    private const int MaxSummaryPairs = 3;
    private const int MaxTooltipPairs = 12;
    private const string NoScalarValueSummary = "No value";
    private const string NoScalarValueTooltip = "No ScalarValue assigned.";

    private readonly Button _editButton;
    private readonly Label _summaryLabel;
    private readonly EditorResourcePicker _resourcePicker;
    private Runtime.ScalarValue _currentScalarValue;

    public ScalarValueReferencePropertyEditor()
    {
        var hbox = new HBoxContainer();
        AddChild(hbox);

        _resourcePicker = new EditorResourcePicker
        {
            BaseType = nameof(Runtime.ScalarValue),
            Editable = true,
            ToggleMode = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _resourcePicker.ResourceChanged += OnResourceChanged;
        _resourcePicker.ResourceSelected += OnResourceSelected;
        hbox.AddChild(_resourcePicker);

        _summaryLabel = new Label
        {
            Text = NoScalarValueSummary,
            TooltipText = NoScalarValueTooltip,
            CustomMinimumSize = new Vector2(140f, 0f),
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Stop
        };
        hbox.AddChild(_summaryLabel);

        _editButton = new Button
        {
            Text = "Edit",
            TooltipText = "Open the dedicated ScalarValue editor"
        };
        _editButton.Pressed += OnEditPressed;
        hbox.AddChild(_editButton);

        UpdateScalarValuePreview();
    }

    public override void _UpdateProperty()
    {
        var editedObject = GetEditedObject();
        if (editedObject == null || !IsInstanceValid(editedObject))
        {
            SetCurrentScalarValue(null);
        }
        else
        {
            var value = editedObject.Get(GetEditedProperty());
            var scalarValue = value.VariantType == Variant.Type.Object
                ? value.AsGodotObject() as Runtime.ScalarValue
                : null;
            SetCurrentScalarValue(scalarValue);
        }

        if (_resourcePicker.EditedResource != _currentScalarValue)
            _resourcePicker.EditedResource = _currentScalarValue;

        _editButton.Text = _currentScalarValue == null ? "New" : "Edit";
        QueueScalarValuePreviewRefresh();
    }

    private void OnResourceChanged(Resource resource)
    {
        SetCurrentScalarValue(resource as Runtime.ScalarValue);
        EmitChanged(GetEditedProperty(), resource);
    }

    private void OnResourceSelected(Resource resource, bool inspect)
    {
        SetCurrentScalarValue(resource as Runtime.ScalarValue);
        _resourcePicker.EditedResource = _currentScalarValue;
        EmitChanged(GetEditedProperty(), resource);

        if (inspect)
            CallDeferred(nameof(OpenCurrentScalarValueDeferred));
    }

    private void OnEditPressed()
    {
        if (_currentScalarValue == null)
        {
            SetCurrentScalarValue(new Runtime.ScalarValue());
            _resourcePicker.EditedResource = _currentScalarValue;
            EmitChanged(GetEditedProperty(), _currentScalarValue);
        }

        CallDeferred(nameof(OpenCurrentScalarValueDeferred));
    }

    private void OpenCurrentScalarValueDeferred()
    {
        var scalarValue = ResolveCurrentScalarValue();
        if (scalarValue == null || !IsInstanceValid(scalarValue))
            return;

        EditorInterface.Singleton.EditResource(scalarValue);
    }

    private Runtime.ScalarValue ResolveCurrentScalarValue()
    {
        if (_currentScalarValue != null && IsInstanceValid(_currentScalarValue))
            return _currentScalarValue;

        var editedObject = GetEditedObject();
        if (editedObject == null || !IsInstanceValid(editedObject))
            return null;

        var value = editedObject.Get(GetEditedProperty());
        if (value.VariantType != Variant.Type.Object)
            return null;

        SetCurrentScalarValue(value.AsGodotObject() as Runtime.ScalarValue);
        return _currentScalarValue;
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        DisconnectCurrentScalarValue();
    }

    private void SetCurrentScalarValue(Runtime.ScalarValue scalarValue)
    {
        if (ReferenceEquals(_currentScalarValue, scalarValue))
        {
            QueueScalarValuePreviewRefresh();
            return;
        }

        DisconnectCurrentScalarValue();
        _currentScalarValue = scalarValue;

        if (_currentScalarValue != null && IsInstanceValid(_currentScalarValue))
            _currentScalarValue.Changed += OnCurrentScalarValueChanged;

        QueueScalarValuePreviewRefresh();
    }

    private void DisconnectCurrentScalarValue()
    {
        if (_currentScalarValue != null && IsInstanceValid(_currentScalarValue))
            _currentScalarValue.Changed -= OnCurrentScalarValueChanged;
    }

    private void OnCurrentScalarValueChanged()
    {
        QueueScalarValuePreviewRefresh();
    }

    private void QueueScalarValuePreviewRefresh()
    {
        UpdateScalarValuePreview();
        CallDeferred(nameof(UpdateScalarValuePreviewDeferred));
    }

    private void UpdateScalarValuePreviewDeferred()
    {
        if (!IsInstanceValid(this))
            return;

        UpdateScalarValuePreview();
    }

    private void UpdateScalarValuePreview()
    {
        if (_summaryLabel == null)
            return;

        _summaryLabel.Text = BuildScalarValueSummary(_currentScalarValue);
        _summaryLabel.TooltipText = BuildScalarValueTooltip(_currentScalarValue);
    }

    private static string BuildScalarValueSummary(Runtime.ScalarValue scalarValue)
    {
        if (scalarValue == null || !IsInstanceValid(scalarValue))
            return NoScalarValueSummary;

        var builder = new StringBuilder();
        var scalar = scalarValue.Get("_scalar").As<float>();
        var curve = scalarValue.Get("_curve").As<Curve>();
        var levelPoints = scalarValue.Get("_levelPoints").As<Array<ScalarValueLevelPoint>>();
        var validPoints = CollectValidLevelPoints(levelPoints);

        builder.Append(FormatNumber(scalar));

        if (validPoints.Count > 0)
        {
            builder.Append(" | ");
            AppendLevelPointPairs(builder, validPoints, MaxSummaryPairs);
        }

        if (curve != null && IsInstanceValid(curve))
        {
            builder.Append(" | Curve");
            if (curve.GetPointCount() > 0)
            {
                builder.Append(" ");
                AppendCurvePointPairs(builder, curve, MaxSummaryPairs);
            }
        }

        return builder.ToString();
    }

    private static string BuildScalarValueTooltip(Runtime.ScalarValue scalarValue)
    {
        if (scalarValue == null || !IsInstanceValid(scalarValue))
            return NoScalarValueTooltip;

        var builder = new StringBuilder();
        builder.Append("ScalarValue");

        if (!string.IsNullOrWhiteSpace(scalarValue.ResourcePath))
            builder.AppendLine().Append("Path: ").Append(scalarValue.ResourcePath);

        var scalar = scalarValue.Get("_scalar").As<float>();
        var curve = scalarValue.Get("_curve").As<Curve>();
        var interpolationMode = (ScalarValueInterpolationMode)scalarValue.Get("_levelInterpolationMode").As<int>();
        var levelPoints = scalarValue.Get("_levelPoints").As<Array<ScalarValueLevelPoint>>();

        builder.AppendLine().Append("Value: ").Append(FormatNumber(scalar));
        AppendLevelTableTooltip(builder, levelPoints, interpolationMode);
        AppendCurveTooltip(builder, curve);

        return builder.ToString();
    }

    private static void AppendLevelTableTooltip(StringBuilder builder, Array<ScalarValueLevelPoint> levelPoints,
        ScalarValueInterpolationMode interpolationMode)
    {
        var validPoints = CollectValidLevelPoints(levelPoints);
        if (validPoints.Count == 0)
            return;

        builder.AppendLine()
            .Append("Level Table (")
            .Append(FormatInterpolationLabel(interpolationMode))
            .Append("): ");

        AppendLevelPointPairs(builder, validPoints, MaxTooltipPairs);
    }

    private static void AppendCurveTooltip(StringBuilder builder, Curve curve)
    {
        if (curve == null || !IsInstanceValid(curve))
            return;

        var pointCount = curve.GetPointCount();
        if (pointCount == 0)
        {
            builder.AppendLine().Append("Curve: assigned");
            return;
        }

        builder.AppendLine().Append("Curve Points: ");
        AppendCurvePointPairs(builder, curve, MaxTooltipPairs);
    }

    private static List<ScalarValueLevelPoint> CollectValidLevelPoints(Array<ScalarValueLevelPoint> levelPoints)
    {
        var validPoints = new List<ScalarValueLevelPoint>();
        if (levelPoints == null || levelPoints.Count == 0)
            return validPoints;

        foreach (var point in levelPoints)
        {
            if (point == null || !IsInstanceValid(point))
                continue;

            validPoints.Add(point);
        }

        validPoints.Sort(static (left, right) => left.Level.CompareTo(right.Level));
        return validPoints;
    }

    private static void AppendCurvePointPairs(StringBuilder builder, Curve curve, int maxPairs)
    {
        var pointCount = curve.GetPointCount();
        var displayCount = Mathf.Min(pointCount, maxPairs);
        for (var index = 0; index < displayCount; index++)
        {
            if (index > 0)
                builder.Append(", ");

            var point = curve.GetPointPosition(index);
            builder.Append(FormatNumber(point.X))
                .Append("=")
                .Append(FormatNumber(point.Y));
        }

        AppendRemainingCount(builder, pointCount, displayCount);
    }

    private static void AppendLevelPointPairs(StringBuilder builder, List<ScalarValueLevelPoint> validPoints,
        int maxPairs)
    {
        var displayCount = Mathf.Min(validPoints.Count, maxPairs);
        for (var index = 0; index < displayCount; index++)
        {
            if (index > 0)
                builder.Append(", ");

            var point = validPoints[index];
            builder.Append(point.Level)
                .Append("=")
                .Append(FormatNumber(point.Value));
        }

        AppendRemainingCount(builder, validPoints.Count, displayCount);
    }

    private static void AppendRemainingCount(StringBuilder builder, int totalCount, int displayCount)
    {
        if (totalCount <= displayCount)
            return;

        builder.Append(", +")
            .Append(totalCount - displayCount)
            .Append(" more");
    }

    private static string FormatInterpolationLabel(ScalarValueInterpolationMode mode)
    {
        return mode switch
        {
            ScalarValueInterpolationMode.EaseInOut => "Ease In Out",
            _ => mode.ToString()
        };
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    void ISerializationListener.OnBeforeSerialize()
    {
        DisconnectCurrentScalarValue();
        _currentScalarValue = null;
    }

    void ISerializationListener.OnAfterDeserialize()
    {
        DisconnectCurrentScalarValue();
        _currentScalarValue = null;
        if (_resourcePicker != null)
        {
            _resourcePicker.EditedResource = null;
        }

        if (_summaryLabel != null)
        {
            _summaryLabel.Text = NoScalarValueSummary;
            _summaryLabel.TooltipText = NoScalarValueTooltip;
        }

        if (_editButton != null)
            _editButton.Text = "New";
    }
}
#endif
