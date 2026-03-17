using Godot;

namespace RhythmicGame;

/// <summary>分数、连击、准确率、血条追踪器。</summary>
public partial class ScoreTracker : Node
{
    public event Action<int, int, double>? ScoreUpdated;
    public event Action<float>? HealthChanged;
    public event Action? ComboBreak;

    // ── 公开状态 ──────────────────────────────────────────────
    public int    Score       { get; private set; } = 0;
    public int    Combo       { get; private set; } = 0;
    public int    MaxCombo    { get; private set; } = 0;
    public float  Health      { get; private set; } = 50f;   // 0~100
    public double Accuracy    { get; private set; } = 1.0;   // 0.0~1.0
    public bool   IsFullCombo { get; private set; } = true;
    public bool   IsAllPerfect{ get; private set; } = true;

    // 各判定计数
    public int CountMaxPerfect { get; private set; }
    public int CountPerfect    { get; private set; }
    public int CountGreat      { get; private set; }
    public int CountGood       { get; private set; }
    public int CountBad        { get; private set; }
    public int CountMiss       { get; private set; }

    // ── 私有 ──────────────────────────────────────────────────
    private int   _totalNotes      = 0;
    private double _noteBaseScore  = 0.0;
    private double _accNumerator   = 0.0;
    private PlayerSettings.FailMode _failMode = PlayerSettings.FailMode.Normal;

    public void Initialize(ChartData chart, PlayerSettings.FailMode failMode)
    {
        ResetState();

        _totalNotes    = chart.TotalNotes;
        _noteBaseScore = _totalNotes > 0 ? 1_000_000.0 / _totalNotes : 0.0;
        _failMode      = failMode;

        Health = failMode switch
        {
            PlayerSettings.FailMode.Hard   => 100f,
            PlayerSettings.FailMode.Easy   => 70f,
            PlayerSettings.FailMode.NoFail => 100f,
            _                               => 50f,
        };

        ScoreUpdated?.Invoke(Score, Combo, Accuracy);
        HealthChanged?.Invoke(Health);
    }

    /// <summary>由 JudgmentSystem.NoteJudged 信号触发</summary>
    public void OnNoteJudged(NoteData note, int judgmentInt, double deltaMs)
    {
        var judgment = (Constants.Judgment)judgmentInt;

        // 更新计数
        switch (judgment)
        {
            case Constants.Judgment.MaxPerfect: CountMaxPerfect++; break;
            case Constants.Judgment.Perfect:    CountPerfect++;    break;
            case Constants.Judgment.Great:      CountGreat++;      break;
            case Constants.Judgment.Good:       CountGood++;       break;
            case Constants.Judgment.Bad:        CountBad++;        IsFullCombo = false; break;
            case Constants.Judgment.Miss:       CountMiss++;       IsFullCombo = false; IsAllPerfect = false; break;
        }

        if (judgment > Constants.Judgment.Perfect)
            IsAllPerfect = false;

        // 分数
        double weight = Constants.JudgmentScoreWeight[judgment];
        Score += (int)(_noteBaseScore * weight);

        // 准确率
        _accNumerator += Constants.JudgmentAccWeight[judgment];
        int judgedCount = CountMaxPerfect + CountPerfect + CountGreat + CountGood + CountBad + CountMiss;
        Accuracy = judgedCount > 0 ? _accNumerator / judgedCount : 1.0;

        // 连击
        if (judgment <= Constants.Judgment.Good)
        {
            Combo++;
            if (Combo > MaxCombo) MaxCombo = Combo;
        }
        else
        {
            if (Combo > 0) ComboBreak?.Invoke();
            Combo = 0;
        }

        // 血条
        UpdateHealth(judgment);

        ScoreUpdated?.Invoke(Score, Combo, Accuracy);
    }

    private void UpdateHealth(Constants.Judgment judgment)
    {
        if (_failMode == PlayerSettings.FailMode.NoFail) return;

        float drain = Constants.HealthDrain[judgment];
        drain *= _failMode switch
        {
            PlayerSettings.FailMode.Easy => Constants.HealthEasyMultiplier,
            PlayerSettings.FailMode.Hard => Constants.HealthHardMultiplier,
            _ => 1f,
        };

        Health = Mathf.Clamp(Health - drain, 0f, 100f);
        HealthChanged?.Invoke(Health);
    }

    private void ResetState()
    {
        Score           = 0;
        Combo           = 0;
        MaxCombo        = 0;
        Health          = 50f;
        Accuracy        = 1.0;
        IsFullCombo     = true;
        IsAllPerfect    = true;
        CountMaxPerfect = 0;
        CountPerfect    = 0;
        CountGreat      = 0;
        CountGood       = 0;
        CountBad        = 0;
        CountMiss       = 0;
        _accNumerator   = 0.0;
    }
}
