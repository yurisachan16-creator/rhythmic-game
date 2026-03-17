using Godot;

namespace RhythmicGame;

/// <summary>玩家设置的读写与广播。注册为 Autoload。</summary>
public partial class SettingsManager : Node
{
    public event Action<string>? SettingsChanged;

    public PlayerSettings Settings { get; private set; } = new();

    public override void _Ready()
    {
        LoadSettings();
        ApplyVolumeSettings();
        RegisterDefaultInputActions();
    }

    // ── 设置读写 ──────────────────────────────────────────────

    public void SetGlobalOffset(float ms)
    {
        Settings.GlobalOffset = ms;
        Save();
        SettingsChanged?.Invoke("GlobalOffset");
    }

    public void SetScrollSpeed(float speed)
    {
        Settings.ScrollSpeed = Mathf.Clamp(speed, 0.5f, 5.0f);
        Save();
        SettingsChanged?.Invoke("ScrollSpeed");
    }

    public void SetMusicVolume(float linear)
    {
        Settings.MusicVolume = Mathf.Clamp(linear, 0f, 1f);
        GetNode<AudioManager>("/root/AudioManager").SetMusicVolume(Settings.MusicVolume);
        Save();
        SettingsChanged?.Invoke("MusicVolume");
    }

    public void SetSfxVolume(float linear)
    {
        Settings.SfxVolume = Mathf.Clamp(linear, 0f, 1f);
        GetNode<AudioManager>("/root/AudioManager").SetSfxVolume(Settings.SfxVolume);
        Save();
        SettingsChanged?.Invoke("SfxVolume");
    }

    public void SetDefaultFailMode(PlayerSettings.FailMode mode)
    {
        Settings.DefaultFailMode = mode;
        Save();
        SettingsChanged?.Invoke("DefaultFailMode");
    }

    public void SetLastSong(string songId, string difficulty)
    {
        Settings.LastSongId    = songId;
        Settings.LastDifficulty = difficulty;
        Save();
    }

    // ── 持久化 ────────────────────────────────────────────────

    private void LoadSettings()
    {
        if (ResourceLoader.Exists(Constants.SettingsPath))
        {
            var loaded = ResourceLoader.Load<PlayerSettings>(Constants.SettingsPath);
            if (loaded is not null) Settings = loaded;
        }
    }

    private void Save()
    {
        ResourceSaver.Save(Settings, Constants.SettingsPath);
    }

    // ── InputMap 初始化 ───────────────────────────────────────

    /// <summary>将玩家键位配置注册到 Godot InputMap</summary>
    public void RegisterDefaultInputActions()
    {
        for (int lane = 0; lane < PlayerSettings.DefaultKeys4K.Length; lane++)
        {
            RegisterLaneAction(lane, 0, PlayerSettings.DefaultKeys4K[lane][0]);
            // 副键暂不绑定（Key.None 跳过）
        }
    }

    private static void RegisterLaneAction(int lane, int slot, Key key)
    {
        if (key == Key.None) return;
        string actionName = $"lane_{lane}_primary";
        if (!InputMap.HasAction(actionName))
            InputMap.AddAction(actionName);

        var ev = new InputEventKey { Keycode = key };
        InputMap.ActionAddEvent(actionName, ev);
    }

    private void ApplyVolumeSettings()
    {
        var audio = GetNode<AudioManager>("/root/AudioManager");
        audio.SetMusicVolume(Settings.MusicVolume);
        audio.SetSfxVolume(Settings.SfxVolume);
    }
}
