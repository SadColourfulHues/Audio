using Godot;
using Godot.Collections;

using System.Runtime.CompilerServices;

namespace SadChromaLib.Audio;

/// <summary>
/// A resource that contains relevant information for a sound effect
/// </summary>
[GlobalClass]
[Tool]
public sealed partial class AudioItem: Resource
{
    [ExportGroup("Sound Effect Info")]
    [Export]
    public string Id;

    [Export]
    public AudioStream SoundEffect = null;

    [Export]
    public AudioStream[] Variations = null;

    [ExportGroup("Properties")]
    [Export(PropertyHint.Range, "0.0, 2.0")]
    public float MinPitch = 0.5f;

    [Export(PropertyHint.Range, "0.0, 2.0")]
    public float MaxPitch = 1.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISoundEffectItem AsSoundEffect()
    {
        if (Variations?.Length < 1) {
            return new SoundEffect(this);
        }

        return new SoundEffectMulti(this);
    }

    #if TOOLS

    // Note: To display item ID in the inspector
    public override void _ValidateProperty(Dictionary property)
    {
        if (!Engine.IsEditorHint())
            return;

        ResourceName = Id ?? "<no id>";
    }

    #endif
}