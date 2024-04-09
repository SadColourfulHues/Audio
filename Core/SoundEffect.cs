using Godot;

using System.Runtime.CompilerServices;

using SadChromaLib.Utils.Random;

namespace SadChromaLib.Audio;

public interface ISoundEffectItem
{
    AudioStream GetStream();
    float GetPitch(float max);

    bool Is(string id);
}

/// <summary>
/// A sound effect that contains one audio stream
/// </summary>
public readonly struct SoundEffect: ISoundEffectItem
{
    public readonly AudioStream Stream;

    public readonly string Id;
    public readonly Vector2 PitchRange;

    public SoundEffect(AudioItem item)
    {
        Stream = item.SoundEffect;
        Id = string.Intern(item.Id);
        PitchRange = new(item.MinPitch, item.MaxPitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Is(string id)
        => Id == id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AudioStream GetStream()
        => Stream;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float GetPitch(float max)
        => Mathf.Clamp(1.0f + (max * RandomUtils.Random(-1.0f, 1.0f)), PitchRange.X, PitchRange.Y);
}

/// <summary>
/// A sound effect that contains an array featuring one or more audio streams
/// </summary>
public readonly struct SoundEffectMulti: ISoundEffectItem
{
    public readonly AudioStream[] Streams;

    public readonly string Id;
    public readonly Vector2 PitchRange;

    public SoundEffectMulti(AudioItem item)
    {
        Streams = item.Variations;

        Id = string.Intern(item.Id);
        PitchRange = new(item.MinPitch, item.MaxPitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Is(string id)
        => Id == id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AudioStream GetStream()
        => RandomUtils.Pick(Streams);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float GetPitch(float max)
        => Mathf.Clamp(1.0f + (max * RandomUtils.Random(-1.0f, 1.0f)), PitchRange.X, PitchRange.Y);
}