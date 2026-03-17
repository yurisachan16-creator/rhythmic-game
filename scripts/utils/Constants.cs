namespace RhythmicGame;

public static class Constants
{
    // ── 判定档位 ──────────────────────────────────────────────────
    public enum Judgment { MaxPerfect, Perfect, Great, Good, Bad, Miss }

    public static readonly Dictionary<Judgment, double> JudgmentScoreWeight = new()
    {
        { Judgment.MaxPerfect, 1.005 },
        { Judgment.Perfect,    1.0   },
        { Judgment.Great,      0.7   },
        { Judgment.Good,       0.4   },
        { Judgment.Bad,        0.2   },
        { Judgment.Miss,       0.0   },
    };

    public static readonly Dictionary<Judgment, double> JudgmentAccWeight = new()
    {
        { Judgment.MaxPerfect, 1.0 },
        { Judgment.Perfect,    1.0 },
        { Judgment.Great,      0.7 },
        { Judgment.Good,       0.4 },
        { Judgment.Bad,        0.2 },
        { Judgment.Miss,       0.0 },
    };

    public static readonly Dictionary<Judgment, string> JudgmentDisplayText = new()
    {
        { Judgment.MaxPerfect, "PERFECT+" },
        { Judgment.Perfect,    "PERFECT"  },
        { Judgment.Great,      "GREAT"    },
        { Judgment.Good,       "GOOD"     },
        { Judgment.Bad,        "BAD"      },
        { Judgment.Miss,       "MISS"     },
    };

    // ── 血条（NORMAL模式，单位%）─────────────────────────────────
    /// <summary>负数 = 回血，正数 = 扣血</summary>
    public static readonly Dictionary<Judgment, float> HealthDrain = new()
    {
        { Judgment.MaxPerfect, -0.5f },
        { Judgment.Perfect,    -0.5f },
        { Judgment.Great,       0.0f },
        { Judgment.Good,        0.0f },
        { Judgment.Bad,         1.0f },
        { Judgment.Miss,        5.0f },
    };

    public const float HealthEasyMultiplier = 0.5f;
    public const float HealthHardMultiplier = 2.0f;

    // ── 滚速 ──────────────────────────────────────────────────────
    /// <summary>基础滚速（像素/秒），玩家倍率在此基础上乘</summary>
    public const float BaseScrollSpeedPx = 400f;
    public const float JudgmentLineYOffset = 120f;

    // ── 预览音乐 ──────────────────────────────────────────────────
    public const float PreviewDelayMs       = 800f;
    public const float PreviewDurationSec   = 15f;
    public const float PreviewFadeDuration  = 0.3f;

    // ── 评级分数线 ────────────────────────────────────────────────
    public static readonly Dictionary<string, int> GradeThresholds = new()
    {
        { "S+", 1_000_000 },
        { "S",  950_000   },
        { "A",  900_000   },
        { "B",  800_000   },
        { "C",  700_000   },
        { "D",  500_000   },
    };

    // ── 场景路径 ──────────────────────────────────────────────────
    public const string SceneMainMenu   = "res://scenes/ui/MainMenu.tscn";
    public const string SceneSongSelect = "res://scenes/ui/SongSelect.tscn";
    public const string SceneGameplay   = "res://scenes/gameplay/GameplayScene.tscn";
    public const string SceneResult     = "res://scenes/ui/ResultScreen.tscn";

    // ── 存档路径 ──────────────────────────────────────────────────
    public const string SaveDir          = "user://save/";
    public const string SettingsPath     = "user://settings.tres";
    public const string RecordsPath      = "user://save/records.json";
    public const string AchievementsPath = "user://save/achievements.json";

    // ── 内置歌曲目录 ──────────────────────────────────────────────
    public const string BuiltinSongsDir = "res://songs/";
}
