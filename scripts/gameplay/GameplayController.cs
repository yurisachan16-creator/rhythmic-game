using Godot;
using System.Linq;

namespace RhythmicGame;

/// <summary>
/// 游玩场景总控制器。
/// 连接各子系统，驱动游玩流程：加载 → 倒计时 → 游玩 → 结算。
/// </summary>
public partial class GameplayController : Node
{
    // 子节点引用（在场景编辑器中绑定）
    [Export] public InputHandler?   InputHandler   { get; set; }
    [Export] public NoteSpawner?    NoteSpawner    { get; set; }
    [Export] public NotePool?       NotePool       { get; set; }
    [Export] public JudgmentSystem? JudgmentSystem { get; set; }
    [Export] public ScoreTracker?   ScoreTracker   { get; set; }
    [Export] public GameplayHUD?    GameplayHud    { get; set; }
    [Export] public Node2D[]        LaneNodes      { get; set; } = [];
    [Export] public string          DebugSongDir   { get; set; } = "res://songs/demo_minimal";
    [Export] public string          DebugChartFile { get; set; } = "chart_easy.json";
    [Export] public string          DebugDifficulty { get; set; } = "DEMO";

    private GameManager?    _game;
    private AudioManager?   _audio;
    private SaveManager?    _save;
    private SettingsManager _settings = null!;

    private SongMeta?  _activeSongMeta;
    private string     _activeDifficulty = "";
    private ChartData? _chart;
    private bool       _isPaused   = false;
    private bool       _hasStarted = false;

    public override void _Ready()
    {
        _game     = GetNode<GameManager>("/root/GameManager");
        _audio    = GetNode<AudioManager>("/root/AudioManager");
        _save     = GetNode<SaveManager>("/root/SaveManager");
        _settings = GetNode<SettingsManager>("/root/SettingsManager");

        InputHandler ??= GetNodeOrNull<InputHandler>("../InputHandler");
        NoteSpawner ??= GetNodeOrNull<NoteSpawner>("../NoteSpawner");
        NotePool ??= GetNodeOrNull<NotePool>("../NotePool");
        JudgmentSystem ??= GetNodeOrNull<JudgmentSystem>("../JudgmentSystem");
        ScoreTracker ??= GetNodeOrNull<ScoreTracker>("../ScoreTracker");
        GameplayHud ??= GetNodeOrNull<GameplayHUD>("../HUD");
        if (LaneNodes.Length == 0)
            LaneNodes = GetNodeOrNull<Node>("../FieldArea")?
                .GetChildren()
                .OfType<Node2D>()
                .Where(child => child.Name.ToString().StartsWith("Lane_"))
                .OrderBy(child => child.Name.ToString())
                .ToArray() ?? [];

        ConnectSignals();
        LoadChart();
    }

    // ── 初始化流程 ─────────────────────────────────────────────

    private void LoadChart()
    {
        (_activeSongMeta, _activeDifficulty) = ResolveSongContext();
        if (_activeSongMeta is null)
        {
            GD.PushError("GameplayController: 无有效的 SongMeta");
            return;
        }

        _chart = ChartLoader.LoadChart(
            _activeSongMeta.SongDir,
            ResolveChartFile());

        if (_chart is null)
        {
            GD.PushError("GameplayController: 加载谱面失败");
            return;
        }

        InputHandler?.SetKeyCount(_chart.KeyCount);
        JudgmentSystem?.Initialize(_chart);
        ScoreTracker?.Initialize(_chart, _settings.Settings.DefaultFailMode);
        NoteSpawner?.Initialize(_chart, NotePool!, GetLaneContainers());

        StartCountdown();
    }

    private async void StartCountdown()
    {
        // TODO: 播放3/2/1倒计时动画
        await ToSignal(GetTree().CreateTimer(3.0), SceneTreeTimer.SignalName.Timeout);
        StartSong();
    }

    private void StartSong()
    {
        if (_activeSongMeta is null || _audio is null) return;

        string audioPath = _activeSongMeta.SongDir
            .PathJoin(_activeSongMeta.AudioFile);

        var stream = ResourceLoader.Load<AudioStream>(audioPath);
        if (stream is null)
        {
            GD.PushError($"GameplayController: 无法加载音频 {audioPath}");
            return;
        }

        _audio.PlaySong(stream);
        _hasStarted = true;
    }

    // ── 暂停 / 恢复 ───────────────────────────────────────────

    public void Pause()
    {
        if (!_hasStarted || _isPaused) return;
        _isPaused = true;
        _audio?.PauseSong();
        GetTree().Paused = true;
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        _audio?.ResumeSong();
        GetTree().Paused = false;
    }

    // ── 结算 ──────────────────────────────────────────────────

    private void OnChartFinished()
    {
        _audio?.StopSong();
        ResultScreenUI.LastTracker = ScoreTracker;
        SaveRecord();
        _game?.GoToResult();
    }

    private void OnHealthZero()
    {
        _audio?.StopSong();
        ResultScreenUI.LastTracker = ScoreTracker;
        // TODO: 播放 Stage Failed 动画后跳转
        _game?.GoToResult();
    }

    private void SaveRecord()
    {
        if (_activeSongMeta is null || ScoreTracker is null) return;
        string chartId = $"{_activeSongMeta.Title}_{_activeDifficulty}";

        _save?.TrySaveRecord(chartId, new SaveManager.RecordEntry(
            Score:       ScoreTracker.Score,
            Accuracy:    ScoreTracker.Accuracy,
            MaxCombo:    ScoreTracker.MaxCombo,
            Grade:       MathUtils.GetGrade(ScoreTracker.Score),
            IsFullCombo: ScoreTracker.IsFullCombo,
            IsAllPerfect:ScoreTracker.IsAllPerfect,
            Timestamp:   DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        ));
    }

    // ── 信号连接 ──────────────────────────────────────────────

    private void ConnectSignals()
    {
        if (InputHandler is not null && JudgmentSystem is not null)
        {
            InputHandler.LanePressed  += JudgmentSystem.OnLanePressed;
            InputHandler.LaneReleased += JudgmentSystem.OnLaneReleased;

            // 轨道视觉反馈
            InputHandler.LanePressed  += OnLanePressed;
            InputHandler.LaneReleased += OnLaneReleased;
        }

        if (JudgmentSystem is not null && ScoreTracker is not null)
            JudgmentSystem.NoteJudged += ScoreTracker.OnNoteJudged;

        if (JudgmentSystem is not null && GameplayHud is not null)
            JudgmentSystem.NoteJudged += OnNoteJudged;

        if (GameplayHud is not null && ScoreTracker is not null)
            GameplayHud.ConnectToTracker(ScoreTracker);

        if (JudgmentSystem is not null && NoteSpawner is not null)
            JudgmentSystem.NoteJudged += NoteSpawner.OnNoteJudged;

        if (NoteSpawner is not null)
            NoteSpawner.ChartFinished += OnChartFinished;

        if (ScoreTracker is not null)
            ScoreTracker.HealthChanged += (h) => { if (h <= 0f) OnHealthZero(); };
    }

    private void OnLanePressed(int lane)
    {
        if (lane < LaneNodes.Length)
            LaneNodes[lane].GetNodeOrNull<Lane>(".")?.OnPressed();
    }

    private void OnLaneReleased(int lane)
    {
        if (lane < LaneNodes.Length)
            LaneNodes[lane].GetNodeOrNull<Lane>(".")?.OnReleased();
    }

    private Node2D[] GetLaneContainers()
    {
        var containers = new Node2D[LaneNodes.Length];
        for (int i = 0; i < LaneNodes.Length; i++)
            containers[i] = LaneNodes[i].GetNode<Node2D>("NoteContainer");
        return containers;
    }

    private (SongMeta? meta, string difficulty) ResolveSongContext()
    {
        if (_game?.CurrentSongMeta is not null)
            return (_game.CurrentSongMeta, _game.CurrentDifficulty);

        var debugMeta = ChartLoader.LoadMeta(DebugSongDir);
        if (debugMeta is null)
        {
            GD.PushError($"GameplayController: 无法加载调试歌曲元数据 {DebugSongDir}");
            return (null, DebugDifficulty);
        }

        return (debugMeta, DebugDifficulty);
    }

    private string ResolveChartFile() =>
        !string.IsNullOrEmpty(_game?.CurrentChartFile)
            ? _game.CurrentChartFile
            : DebugChartFile;

    private void OnNoteJudged(NoteData note, int judgmentInt, double deltaMs)
    {
        GameplayHud?.ShowJudgment((Constants.Judgment)judgmentInt);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
            Pause();
    }
}
