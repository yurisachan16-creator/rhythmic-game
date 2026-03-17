using Godot;

namespace RhythmicGame;

/// <summary>
/// 音符生成调度器。
/// 每帧根据当前歌曲时间决定哪些音符应该出现在屏幕上。
/// </summary>
public partial class NoteSpawner : Node
{
    [Signal] public delegate void ChartFinishedEventHandler();

    private ChartData?  _chart;
    private NotePool?   _pool;
    private Node2D[]?   _laneContainers;  // 每条轨道的 NoteContainer 节点
    private int         _spawnIndex = 0;

    /// <summary>音符出现到判定线所需的时间（毫秒），由滚速计算</summary>
    private double _leadTimeMs = 1000.0;

    private bool _chartEnded = false;

    public void Initialize(ChartData chart, NotePool pool, Node2D[] laneContainers)
    {
        _chart          = chart;
        _pool           = pool;
        _laneContainers = laneContainers;
        _spawnIndex     = 0;
        _chartEnded     = false;
        RecalculateLeadTime();
    }

    public override void _Process(double delta)
    {
        if (_chart is null || _chartEnded) return;

        double nowMs    = GetSongPositionMs();
        double spawnMs  = nowMs + _leadTimeMs;

        // 生成所有应在屏幕上出现的音符
        while (_spawnIndex < _chart.Notes.Count &&
               _chart.Notes[_spawnIndex].TimeMs <= spawnMs)
        {
            SpawnNote(_chart.Notes[_spawnIndex]);
            _spawnIndex++;
        }

        // 检测谱面是否结束（所有音符已超过结束时间）
        if (!_chartEnded && nowMs > _chart.DurationMs + 2000.0)
        {
            _chartEnded = true;
            EmitSignal(SignalName.ChartFinished);
        }
    }

    public void RecalculateLeadTime()
    {
        if (_chart is null) return;
        float scrollSpeed  = GetNode<SettingsManager>("/root/SettingsManager").Settings.ScrollSpeed;
        float screenHeight = GetViewport().GetVisibleRect().Size.Y;
        float travelPx     = screenHeight - Constants.JudgmentLineYOffset;
        float speedPxPerMs = Constants.BaseScrollSpeedPx * scrollSpeed / 1000f;
        _leadTimeMs = travelPx / speedPxPerMs;
    }

    // ── 私有 ──────────────────────────────────────────────────

    private void SpawnNote(NoteData noteData)
    {
        if (_pool is null || _laneContainers is null) return;
        if (noteData.Lane >= _laneContainers.Length) return;

        var noteNode = _pool.GetNote(noteData.Type);
        noteNode.Initialize(noteData, _leadTimeMs);

        _laneContainers[noteData.Lane].AddChild(noteNode);
    }

    private double GetSongPositionMs()
    {
        var audio    = GetNode<AudioManager>("/root/AudioManager");
        var settings = GetNode<SettingsManager>("/root/SettingsManager");
        return audio.GetSongPosition() * 1000.0 + settings.Settings.GlobalOffset;
    }
}
