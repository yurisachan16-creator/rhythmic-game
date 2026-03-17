using Godot;

namespace RhythmicGame;

/// <summary>选曲界面逻辑控制器。挂载到 SongSelect.tscn 根节点。</summary>
public partial class SongSelectUI : Node
{
    // 子节点引用（在场景编辑器中绑定）
    [Export] public Control?   SongList        { get; set; }
    [Export] public Label?     TitleLabel      { get; set; }
    [Export] public Label?     ArtistLabel     { get; set; }
    [Export] public Label?     BpmLabel        { get; set; }
    [Export] public TextureRect? CoverImage    { get; set; }
    [Export] public Control?   DifficultyTabs  { get; set; }
    [Export] public Label?     BestScoreLabel  { get; set; }
    [Export] public Label?     BestAccLabel    { get; set; }
    [Export] public Label?     BestComboLabel  { get; set; }
    [Export] public Label?     GradeLabel      { get; set; }
    [Export] public Button?    StartButton     { get; set; }

    private List<SongMeta> _songs    = [];
    private int            _index    = 0;
    private string         _difficulty = "NORMAL";

    private GameManager?    _game;
    private SaveManager?    _save;
    private SettingsManager? _settings;
    private AudioManager?   _audio;

    // 预览延迟计时
    private SceneTreeTimer? _previewTimer;

    public override void _Ready()
    {
        _game     = GetNode<GameManager>("/root/GameManager");
        _save     = GetNode<SaveManager>("/root/SaveManager");
        _settings = GetNode<SettingsManager>("/root/SettingsManager");
        _audio    = GetNode<AudioManager>("/root/AudioManager");

        _songs = ChartLoader.ScanSongs(Constants.BuiltinSongsDir);
        // TODO: 扫描用户自定义目录并合并

        if (_songs.Count == 0)
        {
            GD.PushWarning("SongSelectUI: 未找到任何歌曲");
            return;
        }

        RestoreLastPosition();
        RefreshAll();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_down")  || IsScrollDown(@event))  Navigate(+1);
        if (@event.IsActionPressed("ui_up")    || IsScrollUp(@event))    Navigate(-1);
        if (@event.IsActionPressed("ui_right"))                           SwitchDifficulty(+1);
        if (@event.IsActionPressed("ui_left"))                            SwitchDifficulty(-1);
        if (@event.IsActionPressed("ui_accept"))                          ConfirmSelection();
        if (@event.IsActionPressed("ui_cancel"))                          _game?.GoToMainMenu();
    }

    // ── 导航 ──────────────────────────────────────────────────

    private void Navigate(int direction)
    {
        if (_songs.Count == 0) return;
        int newIndex = Mathf.Clamp(_index + direction, 0, _songs.Count - 1);
        if (newIndex == _index) return; // 到达边界，触发弹动

        _index = newIndex;
        RefreshAll();
        SchedulePreview();
    }

    private void SwitchDifficulty(int direction)
    {
        if (_songs.Count == 0) return;
        var charts = _songs[_index].Charts;
        if (charts.Count == 0) return;

        int currentIdx = charts.ToList()
            .FindIndex(c => c.GetValueOrDefault("difficulty", "").AsString() == _difficulty);

        int nextIdx = Mathf.Clamp(currentIdx + direction, 0, charts.Count - 1);
        _difficulty = charts[nextIdx].GetValueOrDefault("difficulty", "NORMAL").AsString();
        RefreshRecord();
    }

    private void ConfirmSelection()
    {
        if (_songs.Count == 0) return;
        var meta = _songs[_index];
        var info = meta.GetChartInfo(_difficulty);
        if (info.Count == 0) return;

        _audio?.StopPreview();
        _settings?.SetLastSong(meta.Title, _difficulty);
        _game?.StartSong(meta, _difficulty,
            info.GetValueOrDefault("file", "").AsString());
    }

    // ── 刷新 ──────────────────────────────────────────────────

    private void RefreshAll()
    {
        var meta = _songs[_index];
        if (TitleLabel  is not null) TitleLabel.Text  = meta.GetDisplayTitle();
        if (ArtistLabel is not null) ArtistLabel.Text = meta.GetDisplayArtist();
        if (BpmLabel    is not null) BpmLabel.Text    = $"BPM: {meta.GetBpmDisplay()}";

        // 确保选中的难度在当前歌曲的谱面列表中存在
        var info = meta.GetChartInfo(_difficulty);
        if (info.Count == 0 && meta.Charts.Count > 0)
            _difficulty = meta.Charts[0].GetValueOrDefault("difficulty", "NORMAL").AsString();

        RefreshRecord();

        // TODO: 异步加载封面图
    }

    private void RefreshRecord()
    {
        if (_songs.Count == 0) return;
        var meta    = _songs[_index];
        string id   = $"{meta.Title}_{_difficulty}";
        var record  = _save?.GetBestRecord(id);

        if (record is not null)
        {
            if (BestScoreLabel is not null) BestScoreLabel.Text = record.Score.ToString("N0");
            if (BestAccLabel   is not null) BestAccLabel.Text   = MathUtils.FormatAccuracy(record.Accuracy);
            if (BestComboLabel is not null) BestComboLabel.Text = record.MaxCombo.ToString();
            if (GradeLabel     is not null) GradeLabel.Text     = record.Grade;
        }
        else
        {
            if (BestScoreLabel is not null) BestScoreLabel.Text = "—";
            if (BestAccLabel   is not null) BestAccLabel.Text   = "—";
            if (BestComboLabel is not null) BestComboLabel.Text = "—";
            if (GradeLabel     is not null) GradeLabel.Text     = "—";
        }
    }

    // ── 预览音乐 ──────────────────────────────────────────────

    private void SchedulePreview()
    {
        _previewTimer?.TimeLeft.Equals(0); // 取消上次计时（Godot 无直接取消API，用标记替代）
        _previewTimer = GetTree().CreateTimer(Constants.PreviewDelayMs / 1000.0);
        _previewTimer.Timeout += StartPreview;
    }

    private void StartPreview()
    {
        if (_songs.Count == 0) return;
        var meta       = _songs[_index];
        string path    = meta.SongDir.PathJoin(meta.AudioFile);
        var stream     = ResourceLoader.Load<AudioStream>(path);
        if (stream is null) return;
        _audio?.PlayPreview(stream, meta.PreviewStart);
    }

    // ── 工具 ──────────────────────────────────────────────────

    private void RestoreLastPosition()
    {
        if (_settings is null) return;
        string lastId   = _settings.Settings.LastSongId;
        string lastDiff = _settings.Settings.LastDifficulty;

        int found = _songs.FindIndex(s => s.Title == lastId);
        if (found >= 0)
        {
            _index      = found;
            _difficulty = lastDiff;
        }
    }

    private static bool IsScrollDown(InputEvent e) =>
        e is InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true };

    private static bool IsScrollUp(InputEvent e) =>
        e is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true };
}
