namespace SadChromaLib.Audio;

public enum AudioBus: byte
{
    Master,
    SoundEffects,
    Music,
    UI,
    VoiceLines,
    Others
}

public static class AudioBusUtils
{
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
}