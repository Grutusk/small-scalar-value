
#if TOOLS
using System;
using Godot;
using ScalarValues.Editor;

namespace ScalarValues;

[GlobalClass]
[Tool]
public partial class ScalarValuePlugin : EditorPlugin
{

    [NonSerialized]
    private ScalarValueInspectorPlugin _scalarValueInspectorPlugin;

    public override void _EnterTree()
    {
        if (_scalarValueInspectorPlugin == null)
        {
            _scalarValueInspectorPlugin = new ScalarValueInspectorPlugin();
            AddInspectorPlugin(_scalarValueInspectorPlugin);
        }
    }

    public override void _ExitTree()
    {
        if (_scalarValueInspectorPlugin != null)
        {
            RemoveInspectorPlugin(_scalarValueInspectorPlugin);
            _scalarValueInspectorPlugin = null;
        }

    }


}
#endif