using Godot;

using System;
using System.Diagnostics;

using SadChromaLib.Types;

namespace SadChromaLib.Audio;

/// <summary>
/// A node that handles sound effect playback
/// </summary>
[GlobalClass]
public partial class AudioPlayer: Node
{
    [ExportGroup("Setup")]
    [Export]
    public uint MaxSoundPlayers = 32;

    [Export]
    AudioLibrary _library = null;

    [ExportSubgroup("Shared Properties")]
    [Export(PropertyHint.Range, "0.0,3.0")]
    private float _panningStrength = 1.0f;

    [Export]
    private float _maxDecibels3D = 3f;

    [ExportGroup("Features")]
    [Export]
    public bool SupportsNonPositional = false;

    [Export]
    public bool Supports2D = false;

    [Export]
    public bool Supports3D = false;

    ObjectPool<AudioStreamPlayer> _playersNonPositional = null;
    ObjectPool<AudioStreamPlayer2D> _players2D = null;
    ObjectPool<AudioStreamPlayer3D> _players3D = null;

    public override void _Ready()
    {
        if (SupportsNonPositional) {
            _playersNonPositional = new(MaxSoundPlayers, this);
        }

        if (Supports2D) {
            _players2D = new(MaxSoundPlayers, this);
        }

        if (Supports3D) {
            _players3D = new(MaxSoundPlayers, this);
        }

        Debug.Assert(
            condition: _library is not null,
            message: "AudioPlayer: An audio player needs a valid library resource to function correctly!"
        );

        _library.Initialise();
    }

    #region Main Functions

    /// <summary>
    /// Plays a non-positional sound effect. (Typically used for UIs)
    /// </summary>
    /// <param name="soundId">The sound effect's ID (in the audio library)</param>
    /// <param name="volume">Volume (from 0.0 -> ...)</param>
    /// <param name="maxPitchShift">The maximum amount of pitch shifting allowed (preferrably 0.0...1.0)</param>
    public bool Play(string soundId,
                    float volume = 1.0f,
                    float maxPitchShift = 0.0f,
                    AudioBus bus = AudioBus.UI)
    {
        AudioStreamPlayer player = _playersNonPositional?.Get();

        if (player is null ||
            !_library.TryGet(soundId, out ISoundEffectItem sfx))
        {
            return false;
        }

        player.Stream = sfx.GetStream();
        player.PitchScale = sfx.GetPitch(maxPitchShift);

        player.VolumeDb = Mathf.LinearToDb(volume);
        player.Bus = AudioBusUtils.ToBusName(bus);

        player.Play();

        return true;
    }

    /// <summary>
    /// Plays a sound effect in 2D space.
    /// </summary>
    /// <param name="soundId">The sound effect's ID (in the audio library)</param>
    /// <param name="position">A point in 2D space from where the sound effect should play.</param>
    /// <param name="maxDistance">The maximum distance where the sound effect is still hearable.</param>
    /// <param name="volume">Volume (from 0.0 -> ...)</param>
    /// <param name="maxPitchShift">The maximum amount of pitch shifting allowed (preferrably 0.0...1.0)</param>
    public bool Play(string soundId,
                    Vector2 position,
                    float volume = 1.0f,
                    float maxPitchShift = 0.0f,
                    AudioBus bus = AudioBus.UI,
                    float maxDistance = 256.0f)
    {
        AudioStreamPlayer2D player = _players2D?.Get();

        if (player is null ||
            !_library.TryGet(soundId, out ISoundEffectItem sfx))
        {
            return false;
        }

        player.Stream = sfx.GetStream();
        player.PitchScale = sfx.GetPitch(maxPitchShift);

        player.VolumeDb = Mathf.LinearToDb(volume);
        player.Bus = AudioBusUtils.ToBusName(bus);

        player.MaxDistance = maxDistance;
        player.GlobalPosition = position;

        player.Play();

        return true;
    }

    /// <summary>
    /// Plays a sound effect in 3D space.
    /// </summary>
    /// <param name="soundId">The sound effect's ID (in the audio library)</param>
    /// <param name="position">A point in 2D space from where the sound effect should play.</param>
    /// <param name="maxDistance">The maximum distance where the sound effect is still hearable.</param>
    /// <param name="unitSize">Higher values will make the sound be able to reach greater distances.</param>
    /// <param name="volume">Volume (from 0.0 -> ...)</param>
    /// <param name="maxPitchShift">The maximum amount of pitch shifting allowed (preferrably 0.0...1.0)</param>
    public bool Play(string soundId,
                    Vector3 position,
                    float volume = 1.0f,
                    float maxPitchShift = 0.0f,
                    AudioBus bus = AudioBus.UI,
                    float maxDistance = 256.0f,
                    float unitSize = 10.0f)
    {
        AudioStreamPlayer3D player = _players3D?.Get();

        if (player is null ||
            !_library.TryGet(soundId, out ISoundEffectItem sfx))
        {
            return false;
        }

        player.Stream = sfx.GetStream();
        player.PitchScale = sfx.GetPitch(maxPitchShift);

        player.VolumeDb = Mathf.LinearToDb(volume);
        player.Bus = AudioBusUtils.ToBusName(bus);

        player.UnitSize = unitSize;
        player.MaxDistance = maxDistance;
        player.GlobalPosition = position;

        player.Play();

        return true;
    }

    #endregion

    #region Utils

    /// <summary>
    /// Creates up to a specified amount of audio players in advance.
    /// </summary>
    public void ReservePlayers(uint count)
    {
        count = Math.Min(MaxSoundPlayers, count);

        _playersNonPositional?.Reserve(count);
        _players2D?.Reserve(count);
        _players3D?.Reserve(count);
    }

    /// <summary>
    /// Pre-allocates all audio players at once.
    /// </summary>
    public void ReserveAll()
    {
        _playersNonPositional?.Reserve(MaxSoundPlayers);
        _players2D?.Reserve(MaxSoundPlayers);
        _players3D?.Reserve(MaxSoundPlayers);
    }

    #endregion
}

#region Pool Handlers

partial class AudioPlayer: IObjectPoolHandler<AudioStreamPlayer>
{
    public void ObjectCreated(AudioStreamPlayer @object)
        => AddChild(@object);

    public bool ObjectIsAvailable(AudioStreamPlayer @object)
        => !@object.Playing;
}

partial class AudioPlayer: IObjectPoolHandler<AudioStreamPlayer2D>
{
    public void ObjectCreated(AudioStreamPlayer2D @object)
    {
        @object.PanningStrength = _panningStrength;
        AddChild(@object);
    }

    public bool ObjectIsAvailable(AudioStreamPlayer2D @object)
        => !@object.Playing;
}

partial class AudioPlayer: IObjectPoolHandler<AudioStreamPlayer3D>
{
    public void ObjectCreated(AudioStreamPlayer3D @object)
    {
        @object.MaxDb = _maxDecibels3D;
        @object.PanningStrength = _panningStrength;

        AddChild(@object);
    }

    public bool ObjectIsAvailable(AudioStreamPlayer3D @object)
        => !@object.Playing;
}

#endregion