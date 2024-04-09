using Godot;

namespace SadChromaLib.Audio;

/// <summary>
/// A resource that contains relevant info about a BGM track
/// </summary>
[GlobalClass]
public sealed partial class MusicItem: Resource
{
    [Export]
    public string Id;

    [Export]
    public AudioStream Stream;

    public BgmTrack AsTrack()
        => new(this);
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