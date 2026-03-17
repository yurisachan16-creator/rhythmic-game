using Godot;

namespace RhythmicGame;

[GlobalClass]
public partial class NoteData : Resource
{
    public enum NoteType { Tap, Hold, Slide }

    [Export] public NoteType Type { get; set; } = NoteType.Tap;
    [Export] public int Lane { get; set; } = 0;
    [Export] public double Beat { get; set; } = 0.0;

    /// <summary>尾部拍位（Hold / Slide 使用，Tap 忽略）</summary>
    [Export] public double EndBeat { get; set; } = 0.0;

    /// <summary>Slide 终点轨道，其余为 -1</summary>
    [Export] public int LaneEnd { get; set; } = -1;

    /// <summary>由 BeatCalculator 填入，判定系统只使用这两个值</summary>
    public double TimeMs { get; set; } = 0.0;
    public double EndTimeMs { get; set; } = 0.0;

    /// <summary>运行时状态，由 JudgmentSystem 维护</summary>
    public bool IsJudged { get; set; } = false;
    public bool IsHoldActive { get; set; } = false;
}
