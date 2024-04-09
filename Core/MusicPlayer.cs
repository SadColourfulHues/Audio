using Godot;

using System.Diagnostics;

namespace SadChromaLib.Audio;

/// <summary>
/// A node that handles music playback [with crossfade]
/// </summary>
[GlobalClass]
public sealed partial class MusicPlayer: Node
{
    [ExportGroup("Setup")]
    [Export]
    MusicLibrary _library;

    [ExportGroup("Crossfade")]
    [Export]
    float _crossfadeDuration = 2.0f;

    [Export]
    float _trackTransitionTickThreshold = 0.5f;

    Tween _fadeTween;
    AudioStreamPlayer _sourcePlayer;
    AudioStreamPlayer _fadePlayer;

    bool _isFading;
    string _currentBgmId;
    string _targetBgmId;

    float _checkTick;

    public override void _Ready()
    {
        _checkTick = 0.0f;
        _isFading = false;
        _currentBgmId = null;
        _targetBgmId = null;

        Debug.Assert(
            condition: _library is not null,
            message: "MusicPlayer: A music player needs a valid library resource to function correctly!"
        );

        _library.Initialise();

        string busName = AudioBusUtils.ToBusName(AudioBus.Music);
        _sourcePlayer = new() { Name = "Source", Bus = busName };
        _fadePlayer = new() { Name = "Fade", Bus = busName };

        AddChild(_sourcePlayer);
        AddChild(_fadePlayer);

        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        _checkTick += (float) delta;

        if (_checkTick < _trackTransitionTickThreshold)
            return;

        _checkTick = 0.0f;

        if (_targetBgmId is null || _currentBgmId == _targetBgmId)
            return;

        StartFade();
        SetStream(_targetBgmId, _sourcePlayer);

        _currentBgmId = _targetBgmId;
    }

    #region Main Functions

    public void SetTrack(string bgmId)
    {
        if (_currentBgmId is null) {
            _currentBgmId = bgmId;

            StartFade();
            SetStream(bgmId, _sourcePlayer);
            return;
        }

        _targetBgmId = bgmId;
        SetProcess(true);
    }

    #endregion

    #region Utils

    private void SetStream(string id, AudioStreamPlayer player)
    {
        if (!_library.TryGet(id, out BgmTrack track))
            return;

        player.Stream = track.Stream;
        player.Play();
    }

    private void TweenFadeCallback(float f)
    {
        _fadePlayer.VolumeDb = Mathf.LinearToDb(1.0f - f);
        _sourcePlayer.VolumeDb = Mathf.LinearToDb(f);
    }

    /// <summary>
    /// Note to future self:
    /// This method must be called before updating the source player's stream!
    /// </summary>
    private void StartFade()
    {
        if (_isFading)
            return;

        _isFading = true;

        // Fade out the currently-playing stream
        _fadePlayer.Stream = _sourcePlayer.Stream;
        _fadePlayer.Play();

        // Synchronise player positions to make the transition as seamless as possible
        _fadePlayer.Seek(_sourcePlayer.GetPlaybackPosition());

        // Reset fade state
        _fadePlayer.VolumeDb = Mathf.LinearToDb(1.0f);
        _sourcePlayer.VolumeDb = Mathf.LinearToDb(0.0f);

        if (IsInstanceValid(_fadeTween) && _fadeTween.IsRunning()) {
            _fadeTween.Kill();
        }

        _fadeTween = CreateTween();

        _fadeTween
            .TweenMethod(
                method: Callable.From((float fac) => TweenFadeCallback(fac)),
                from: 0.0f, to: 1.0f,
                duration: _crossfadeDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);

        _fadeTween.TweenCallback(Callable.From(FinaliseFade));
    }

    private void FinaliseFade()
    {
        _fadePlayer.Stop();
        _isFading = false;
    }

    #endregion
}