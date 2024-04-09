using Godot;
using System;

namespace SadChromaLib.Audio;

/// <summary>
/// A resource that contains a list of music audio entries.
/// </summary>
[GlobalClass]
public sealed partial class MusicLibrary: Resource
{
    [Export]
    MusicItem[] _musicItems;

    BgmTrack[] _tracks;

    bool _initialised = false;

    #region Main Functions

    public void Initialise()
    {
        if (_initialised)
            return;

        int count = _musicItems.Length;
        _tracks = new BgmTrack[count];

        for (int i = 0; i < count; ++i) {
            _tracks[i] = _musicItems[i].AsTrack();
        }

        _initialised = true;
    }

    /// <summary>
    /// Returns an BGM item with a given ID.
    /// </summary>
    public bool TryGet(string id, out BgmTrack track)
    {
        track = default;
        ReadOnlySpan<BgmTrack> tracks = _tracks.AsSpan();

        for (int i = 0; i < tracks.Length; ++i) {
            if (tracks[i].Id != id)
                continue;

            track = tracks[i];
            return true;
        }

        return true;
    }

    #endregion

    public ReadOnlySpan<BgmTrack> GetTracks()
        => _tracks.AsSpan();
}