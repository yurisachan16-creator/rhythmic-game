using Godot;
using Godot.Collections;

namespace RhythmicGame;

[GlobalClass]
public partial class PlayerSettings : Resource
{
    /// <summary>
    /// 键位绑定：外层数组为轨道，内层数组为该轨道绑定的 InputMap action 名（最多2个）
    /// </summary>
    [Export]
    public Godot.Collections.Array<Godot.Collections.Array> KeyBindings { get; set; } =
    [
        new Godot.Collections.Array { "lane_0_primary", "lane_0_secondary" },
        new Godot.Collections.Array { "lane_1_primary", "lane_1_secondary" },
        new Godot.Collections.Array { "lane_2_primary", "lane_2_secondary" },
        new Godot.Collections.Array { "lane_3_primary", "lane_3_secondary" },
    ];

    /// <summary>默认4K键位对应的 Godot Key 枚举</summary>
    public static readonly Key[][] DefaultKeys4K =
    [
        [Key.D, Key.None],
        [Key.F, Key.None],
        [Key.J, Key.None],
        [Key.K, Key.None],
    ];

    /// <summary>全局判定偏移（毫秒），正值表示玩家习惯早按</summary>
    [Export] public float GlobalOffset { get; set; } = 0f;

    [Export] public float ScrollSpeed { get; set; } = 1.5f;
    [Export] public float MusicVolume { get; set; } = 0.8f;
    [Export] public float SfxVolume { get; set; } = 1.0f;

    /// <summary>默认血条模式</summary>
    [Export] public FailMode DefaultFailMode { get; set; } = FailMode.Normal;

    /// <summary>记住上次选曲位置</summary>
    [Export] public string LastSongId { get; set; } = "";
    [Export] public string LastDifficulty { get; set; } = "NORMAL";

    public enum FailMode { Easy, Normal, Hard, NoFail }
}
