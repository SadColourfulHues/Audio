using Godot;
using Godot.Collections;

namespace SadChromaLib.Audio;

/// <summary>
/// A resource that contains relevant info about a BGM track
/// </summary>
[GlobalClass]
#if TOOLS
[Tool]
#endif
public sealed partial class MusicItem: Resource
{
    [Export]
    public string Id;

    [Export]
    public AudioStream Stream;

    public BgmTrack AsTrack()
        => new(this);

    #if TOOLS
    // Note: To display item ID in the inspector
    public override void _ValidateProperty(Dictionary property)
    {
        if (!Engine.IsEditorHint())
            return;

        ResourceName = Id ?? "<no id>";
        EmitChanged();
    }
    #endif
}

public readonly struct BgmTrack
{
    public readonly string Id;
    public readonly AudioStream Stream;

    public BgmTrack(MusicItem item)
    {
        Id = string.Intern(item.Id);
        Stream = item.Stream;
    }
}