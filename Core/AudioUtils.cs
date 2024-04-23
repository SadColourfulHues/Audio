using System;
using System.Runtime.CompilerServices;
using Godot;

namespace SadChromaLib.Audio;

public static class AudioUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBusName(AudioBus bus)
    {
        return bus switch {
            AudioBus.SoundEffects => "sfx",
            AudioBus.Music => "bgm",
            AudioBus.UI => "ui",
            AudioBus.VoiceLines => "voices",
            AudioBus.Others => "others",
            _ => "Master"
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetVolume(AudioBus bus, float volume)
    {
        AudioServer.SetBusVolumeDb(
            busIdx: AudioServer.GetBusIndex(ToBusName(bus)),
            volumeDb: AsDb(volume)
        );
    }

    /// <summary>
    /// (no neg checking)
    /// A C# implementation of Godot's linear_to_db function.
    /// https://github.com/godotengine/godot/blob/7abe0c6014022874378cb64a11b26b0f0f178324/core/math/math_funcs.h#L483
    /// </summary>
    /// <param name="volume"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AsDb(float volume)
        // (fix) Infinitesimals does the intended behaviour when volume == 0
        => MathF.Log(MathF.Max(0.001f, volume)) * 8.6858896380650365530225783783321f;
}