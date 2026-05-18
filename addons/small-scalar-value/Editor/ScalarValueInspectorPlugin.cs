#if TOOLS
using Godot;
using ScalarValues.Runtime;

namespace ScalarValues.Editor;

[GlobalClass]
[Tool]
public partial class ScalarValueInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object)
    {
        return true;
    }

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name,
        PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
    {
        if (@object is ScalarValue)
        {
            if (name == "_scalar")
            {
                AddPropertyEditor(name, new ScalarValuePropertyEditor());
                return true;
            }

            if (name is "_curve" or "_levelInterpolationMode" or "_levelPoints")
                return true;
        }

        if (type == Variant.Type.Object &&
            hintString.Contains(nameof(ScalarValue)) &&
            !hintString.Contains(nameof(ScalarValueLevelPoint)))
        {
            AddPropertyEditor(name, new ScalarValueReferencePropertyEditor());
            return true;
        }

        return false;
    }
}
#endif
