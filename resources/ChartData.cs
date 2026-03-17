using Godot;
using Godot.Collections;

namespace RhythmicGame;

[GlobalClass]
public partial class ChartData : Resource
{
    [Export] public int Version { get; set; } = 1;
    [Export] public int KeyCount { get; set; } = 4;

    /// <summary>Overall Difficulty (0~10)，影响判定窗口严格度</summary>
    [Export] public float Od { get; set; } = 8f;

    /// <summary>BPM 变化事件列表，元素为 { beat: float, bpm: float }</summary>
    [Export] public Godot.Collections.Array<Godot.Collections.Dictionary> BpmEvents { get; set; } = [];

    /// <summary>视觉滚速变化事件列表，元素为 { beat: float, speed: float }</summary>
    [Export] public Godot.Collections.Array<Godot.Collections.Dictionary> ScrollEvents { get; set; } = [];

    /// <summary>所有音符，按 TimeMs 升序排列（由 ChartLoader 保证）</summary>
    [Export] public Godot.Collections.Array<NoteData> Notes { get; set; } = [];

    /// <summary>谱面演出事件</summary>
    [Export] public Godot.Collections.Array<Godot.Collections.Dictionary> Events { get; set; } = [];

    // ── 运行时字段（由 ChartLoader 填入）──────────────────────────
    public int TotalNotes { get; set; } = 0;       // 总音符当量
    public double DurationMs { get; set; } = 0.0;
    public string SourcePath { get; set; } = "";

    /// <summary>根据 OD 返回各判定档位的时间窗口（毫秒）</summary>
    public System.Collections.Generic.Dictionary<Constants.Judgment, double> GetJudgmentWindows()
    {
        double t = Od / 10.0;
        return new System.Collections.Generic.Dictionary<Constants.Judgment, double>
        {
            { Constants.Judgment.MaxPerfect, Mathf.Lerp(32.0f, 16.0f, (float)t) },
            { Constants.Judgment.Perfect,    Mathf.Lerp(80.0f, 40.0f, (float)t) },
            { Constants.Judgment.Great,      Mathf.Lerp(120.0f, 75.0f, (float)t) },
            { Constants.Judgment.Good,       Mathf.Lerp(180.0f, 110.0f, (float)t) },
            { Constants.Judgment.Bad,        Mathf.Lerp(240.0f, 150.0f, (float)t) },
        };
    }
}
