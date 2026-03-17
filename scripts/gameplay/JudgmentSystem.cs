using Godot;
using System.Collections.Generic;

namespace RhythmicGame;

/// <summary>
/// 判定系统。接收输入事件，对比当前歌曲时间与音符时间，给出判定结果。
/// </summary>
public partial class JudgmentSystem : Node
{
    [Signal]
    public delegate void NoteJudgedEventHandler(
        NoteData note,
        int judgment,        // Constants.Judgment 强转为 int（GDSignal 限制）
        double deltaMs);     // 正=晚，负=早

    private ChartData? _chart;
    private Dictionary<Constants.Judgment, double> _windows = [];

    /// <summary>每条轨道当前"待判定音符"的索引，避免每帧全量搜索</summary>
    private int[] _nextNoteIndex = [];

    public void Initialize(ChartData chart)
    {
        _chart          = chart;
        _windows        = chart.GetJudgmentWindows();
        _nextNoteIndex  = new int[chart.KeyCount];
    }

    /// <summary>由 InputHandler.LanePressed 信号触发</summary>
    public void OnLanePressed(int lane)
    {
        if (_chart is null) return;
        double nowMs = GetSongPositionMs();
        TryJudgeNote(lane, nowMs, isRelease: false);
    }

    /// <summary>由 InputHandler.LaneReleased 信号触发（处理 Hold 尾部）</summary>
    public void OnLaneReleased(int lane)
    {
        if (_chart is null) return;
        double nowMs = GetSongPositionMs();
        TryJudgeHoldTail(lane, nowMs);
    }

    /// <summary>每帧检查超时未按的音符（Miss）</summary>
    public override void _Process(double delta)
    {
        if (_chart is null) return;
        double nowMs = GetSongPositionMs();

        for (int lane = 0; lane < _chart.KeyCount; lane++)
            CheckMiss(lane, nowMs);
    }

    // ── 私有判定逻辑 ──────────────────────────────────────────

    private void TryJudgeNote(int lane, double nowMs, bool isRelease)
    {
        var note = FindNextNote(lane);
        if (note is null) return;

        double deltaMs = nowMs - note.TimeMs;
        double absDelta = Math.Abs(deltaMs);

        var judgment = GetJudgment(absDelta);
        if (judgment == Constants.Judgment.Miss) return; // 在窗口外，不处理

        if (note.Type == NoteData.NoteType.Tap)
        {
            MarkJudged(note, lane);
            EmitSignal(SignalName.NoteJudged, note, (int)judgment, deltaMs);
        }
        else if (note.Type is NoteData.NoteType.Hold or NoteData.NoteType.Slide)
        {
            // 头部命中，开始持续追踪
            note.IsHoldActive = true;
            EmitSignal(SignalName.NoteJudged, note, (int)judgment, deltaMs);
        }
    }

    private void TryJudgeHoldTail(int lane, double nowMs)
    {
        var note = FindActiveHold(lane);
        if (note is null) return;

        double deltaMs = nowMs - note.EndTimeMs;
        double absDelta = Math.Abs(deltaMs);

        // 尾部早放：降一档；超时放：不惩罚（CheckMiss 处理）
        var judgment = deltaMs < -_windows[Constants.Judgment.Good]
            ? DowngradeJudgment(GetJudgment(absDelta))
            : GetJudgment(absDelta);

        MarkJudged(note, lane);
        EmitSignal(SignalName.NoteJudged, note, (int)judgment, deltaMs);
    }

    private void CheckMiss(int lane, double nowMs)
    {
        var note = FindNextNote(lane);
        if (note is null) return;

        double deltaMs = nowMs - note.TimeMs;
        if (deltaMs > _windows[Constants.Judgment.Bad])
        {
            MarkJudged(note, lane);
            EmitSignal(SignalName.NoteJudged, note, (int)Constants.Judgment.Miss, deltaMs);
        }
    }

    private Constants.Judgment GetJudgment(double absDelta)
    {
        if (absDelta <= _windows[Constants.Judgment.MaxPerfect]) return Constants.Judgment.MaxPerfect;
        if (absDelta <= _windows[Constants.Judgment.Perfect])    return Constants.Judgment.Perfect;
        if (absDelta <= _windows[Constants.Judgment.Great])      return Constants.Judgment.Great;
        if (absDelta <= _windows[Constants.Judgment.Good])       return Constants.Judgment.Good;
        if (absDelta <= _windows[Constants.Judgment.Bad])        return Constants.Judgment.Bad;
        return Constants.Judgment.Miss;
    }

    private static Constants.Judgment DowngradeJudgment(Constants.Judgment j) =>
        j < Constants.Judgment.Bad ? j + 1 : Constants.Judgment.Bad;

    private NoteData? FindNextNote(int lane)
    {
        if (_chart is null) return null;
        int idx = _nextNoteIndex[lane];
        while (idx < _chart.Notes.Count)
        {
            var note = _chart.Notes[idx];
            if (note.Lane == lane && !note.IsJudged && !note.IsHoldActive)
                return note;
            idx++;
        }
        return null;
    }

    private NoteData? FindActiveHold(int lane)
    {
        if (_chart is null) return null;
        foreach (var note in _chart.Notes)
            if (note.Lane == lane && note.IsHoldActive && !note.IsJudged)
                return note;
        return null;
    }

    private void MarkJudged(NoteData note, int lane)
    {
        note.IsJudged     = true;
        note.IsHoldActive = false;
        _nextNoteIndex[lane]++;
    }

    private double GetSongPositionMs()
    {
        var audio    = GetNode<AudioManager>("/root/AudioManager");
        var settings = GetNode<SettingsManager>("/root/SettingsManager");
        return audio.GetSongPosition() * 1000.0 + settings.Settings.GlobalOffset;
    }
}
