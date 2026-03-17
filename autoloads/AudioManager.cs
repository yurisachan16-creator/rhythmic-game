using Godot;

namespace RhythmicGame;

/// <summary>音乐播放 + 精确时间戳。注册为 Autoload。</summary>
public partial class AudioManager : Node
{
    private AudioStreamPlayer _musicPlayer = null!;
    private AudioStreamPlayer _sfxPlayer   = null!;

    public bool IsPlaying => _musicPlayer.Playing;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _musicPlayer = new AudioStreamPlayer { Bus = "Music" };
        _sfxPlayer   = new AudioStreamPlayer { Bus = "SFX"   };
        AddChild(_musicPlayer);
        AddChild(_sfxPlayer);
    }

    // ── 歌曲时间（最关键的方法）────────────────────────────────
    /// <summary>
    /// 返回当前歌曲精确播放位置（秒）。
    /// 补偿音频缓冲延迟，比 GetPlaybackPosition() 精度更高。
    /// </summary>
    public double GetSongPosition()
    {
        if (!_musicPlayer.Playing) return 0.0;
        return _musicPlayer.GetPlaybackPosition()
             + AudioServer.GetTimeSinceLastMix()
             - AudioServer.GetOutputLatency();
    }

    // ── 歌曲控制 ──────────────────────────────────────────────
    public void PlaySong(AudioStream stream, float startOffset = 0f)
    {
        _musicPlayer.Stream = stream;
        _musicPlayer.Play(startOffset);
    }

    public void StopSong() => _musicPlayer.Stop();

    public void PauseSong()
    {
        if (_musicPlayer.Playing)
            _musicPlayer.StreamPaused = true;
    }

    public void ResumeSong() => _musicPlayer.StreamPaused = false;

    // ── 预览音乐（选曲界面用）────────────────────────────────
    private Tween? _previewTween;

    public void PlayPreview(AudioStream stream, float startSec, float targetVolume = 0.8f)
    {
        _previewTween?.Kill();
        _musicPlayer.Stop();
        _musicPlayer.Stream = stream;
        _musicPlayer.VolumeDb = -80f;
        _musicPlayer.Play(startSec);

        _previewTween = CreateTween();
        _previewTween.TweenProperty(_musicPlayer, "volume_db",
            Mathf.LinearToDb(targetVolume), Constants.PreviewFadeDuration);
    }

    public void StopPreview()
    {
        _previewTween?.Kill();
        _previewTween = CreateTween();
        _previewTween.TweenProperty(_musicPlayer, "volume_db", -80f, Constants.PreviewFadeDuration);
        _previewTween.TweenCallback(Callable.From(_musicPlayer.Stop));
    }

    // ── SFX ──────────────────────────────────────────────────
    public void PlayHitSfx(AudioStream stream)
    {
        _sfxPlayer.Stream = stream;
        _sfxPlayer.Play();
    }

    // ── 音量控制 ──────────────────────────────────────────────
    public void SetMusicVolume(float linear)
    {
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("Music"),
            Mathf.LinearToDb(linear));
    }

    public void SetSfxVolume(float linear)
    {
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("SFX"),
            Mathf.LinearToDb(linear));
    }
}
