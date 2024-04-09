using Godot;
using System;

namespace SadChromaLib.Audio;

/// <summary>
/// A resource that contains a collection of playable audio streams.
/// </summary>
[GlobalClass]
public sealed partial class AudioLibrary: Resource
{
    [Export]
    AudioItem[] _soundEffects;

    ISoundEffectItem[] _items;
    bool _initialised;

    public AudioLibrary() {
        _initialised = false;
    }

    #region Main Functions

    public void Initialise()
    {
        if (_initialised)
            return;

        int count = _soundEffects.Length;
        _items = new ISoundEffectItem[count];

        for (int i = 0; i < count; ++i) {
            _items[i] = _soundEffects[i].AsSoundEffect();
        }

        _initialised = true;
    }

    /// <summary>
    /// Returns an audio item with the given ID.
    /// </summary>
    public bool TryGet(string id, out ISoundEffectItem item)
    {
        ReadOnlySpan<ISoundEffectItem> items = _items.AsSpan();
        item = default;

        for (int i = 0; i < items.Length; ++i) {
            if (!items[i].Is(id))
                continue;

            item = items[i];
            return true;
        }

        return false;
    }

    #endregion
}