using Godot;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SadChromaLib.Audio;

/// <summary>
/// A node that handles music playback [with crossfade]
/// </summary>
[GlobalClass]
public sealed partial class MusicPlayer: Node
{
    /// <summary>
    /// If 'Watch for Beats' is enabled, this event is triggered whenever a beat
    /// has elapsed during playback.
    ///
    /// <para>
    ///     OnBeatTick: (int) inBarBeat, (int) totalBeat
    /// </para>
    /// </summary>
    public event Action<int, int> OnBeatTick;

    /// <summary>
    /// If 'Watch for Beats' is enabled, this event is triggered whenever a bar
    /// has elapsed during playback.
    ///
    /// <para>
    ///     OnBarTick: (int) barIdx
    /// </para>
    /// </summary>
    public event Action<int> OnBarTick;

    [ExportGroup("Setup")]
    [Export]
    MusicLibrary _library;

    [Export]
    bool _watchForBeats;

    [ExportGroup("Crossfade")]
    [Export]
    float _crossfadeDuration = 2.0f;

    [Export]
    float _trackTransitionTickThreshold = 0.5f;

    [Export]
    Tween.EaseType _crossfadeEase = Tween.EaseType.InOut;

    [Export]
    Tween.TransitionType _crossfadeTrans = Tween.TransitionType.Sine;

    Tween _fadeTween;
    AudioStreamPlayer _sourcePlayer;
    AudioStreamPlayer _fadePlayer;

    bool _isFading;
    string _currentBgmId;
    string _targetBgmId;

    double _bps;
    int _barBeats;
    float _checkTick;

    int _lastBeat;
    int _lastBar;

    public override void _Ready()
    {
        _checkTick = 0.0f;
        _isFading = false;
        _currentBgmId = null;
        _targetBgmId = null;

        _bps = 0.0;
        _barBeats = 4;

        #if TOOLS
        Debug.Assert(
            condition: _library is not null,
            message: "MusicPlayer: A music player needs a valid library resource to function correctly!"
        );
        #endif

        _library.Initialise();

        string busName = AudioUtils.ToBusName(AudioBus.Music);
        _sourcePlayer = new() { Name = "Source", Bus = busName };
        _fadePlayer = new() { Name = "Fade", Bus = busName };

        AddChild(_sourcePlayer);
        AddChild(_fadePlayer);
    }

    public override void _Process(double deltaD)
    {
        float delta = (float) deltaD;

        WatchForTransitionRequest(delta);

        if (!_watchForBeats)
            return;

        WatchForBeats(delta);
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
    }

    /// <summary>
    /// Stops the current background track
    /// </summary>
    public void Stop()
    {
        if (_isFading || !_sourcePlayer.Playing)
            return;

        StartOrReuseTween();
        _isFading = true;

        _fadeTween
            .TweenProperty(
                @object: _sourcePlayer,
                property: AudioStreamPlayer.PropertyName.VolumeDb.ToString(),
                finalVal: AudioUtils.AsDb(0.001f),
                duration: _crossfadeDuration
            )
            .SetTrans(_crossfadeTrans)
            .SetEase(_crossfadeEase);

        // Reverts to its default state basically
        _fadeTween.TweenCallback(Callable.From(() => {
            _isFading = false;

            _sourcePlayer.Stop();
            _fadePlayer.Stop();

            _sourcePlayer.Stream = null;
            _fadePlayer.Stream = null;

            _currentBgmId = null;
            _targetBgmId = null;
        }));
    }

    #endregion

    #region Watchers

    private void WatchForTransitionRequest(float delta)
    {
        _checkTick += delta;

        if (_checkTick < _trackTransitionTickThreshold)
            return;

        _checkTick = 0.0f;

        if (_targetBgmId is null || _currentBgmId == _targetBgmId)
            return;

        StartFade();
        SetStream(_targetBgmId, _sourcePlayer);

        _currentBgmId = _targetBgmId;
    }

    private void WatchForBeats(float delta)
    {
        if (_bps < 0.5f || !_sourcePlayer.Playing)
            return;

        double position = _sourcePlayer.GetPlaybackPosition();

        int estimatedBeatIdx = (int) Math.Floor(position * _bps);
        int estimatedBar = (int) Math.Floor((float) estimatedBeatIdx / _barBeats);

        if (estimatedBeatIdx != _lastBeat) {
            OnBeatTick?.Invoke(estimatedBeatIdx % _barBeats, estimatedBeatIdx);
            _lastBeat = estimatedBeatIdx;
        }

        if (estimatedBar != _lastBar) {
            OnBarTick?.Invoke(estimatedBar);
            _lastBar = estimatedBar;
        }
    }

    #endregion

    #region Utils

    /// <summary>
    /// (no-checking) Returns the current beat index of the currently-playing track
    /// </summary>
    /// <returns></returns>
    public float GetCurrentBeatIdx() {
        return (int) Math.Floor(_sourcePlayer.GetPlaybackPosition() * _bps);
    }

    /// <summary>
    /// (no-checking) Returns the current bar index of the currently-playing track
    /// </summary>
    /// <returns></returns>
    public float GetCurrentBarIdx() {
        return (int) Math.Floor(_sourcePlayer.GetPlaybackPosition() * _bps) / _barBeats;
    }

    private void SetStream(string id, AudioStreamPlayer player)
    {
        const double BpsFac = 1.0 / 60.0;

        if (!_library.TryGet(id, out BgmTrack track))
            return;

        player.Stream = track.Stream;
        player.Play();

        _lastBar = 0;
        _lastBeat = 0;

        // Update track info
        switch (track.Stream)
        {
            case AudioStreamMP3 streamMp3:
                _bps = streamMp3.Bpm * BpsFac;
                _barBeats = streamMp3.BarBeats;
                break;

            case AudioStreamOggVorbis streamOgg:
                _bps = streamOgg.Bpm * BpsFac;
                _barBeats = streamOgg.BarBeats;
                break;

            default:
                _bps = 0.0f;
                _barBeats = 4;
                break;
        }
    }

    private void StartOrReuseTween()
    {
        if (IsInstanceValid(_fadeTween) && _fadeTween.IsRunning()) {
            _fadeTween.Kill();
        }

        _fadeTween = CreateTween();
    }

    private void TweenFadeCallback(float f)
    {
        _fadePlayer.VolumeDb = AudioUtils.AsDb(1.0f - f);
        _sourcePlayer.VolumeDb = AudioUtils.AsDb(f);
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
        _fadePlayer.VolumeDb = AudioUtils.AsDb(1.0f);
        _sourcePlayer.VolumeDb = AudioUtils.AsDb(0.0f);

        StartOrReuseTween();

        _fadeTween
            .TweenMethod(
                method: Callable.From((float fac) => TweenFadeCallback(fac)),
                from: 0.0f, to: 1.0f,
                duration: _crossfadeDuration)
            .SetTrans(_crossfadeTrans)
            .SetEase(_crossfadeEase);

        _fadeTween.TweenCallback(Callable.From(FinaliseFade));
    }

    private void FinaliseFade()
    {
        _fadePlayer.Stop();
        _isFading = false;
    }

    #endregion
}