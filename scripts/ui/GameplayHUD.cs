using Godot;

namespace RhythmicGame;

/// <summary>游玩中的 HUD，监听 ScoreTracker 信号并刷新显示。</summary>
public partial class GameplayHUD : CanvasLayer
{
    [Export] public Label?        ScoreLabel      { get; set; }
    [Export] public Label?        ComboLabel      { get; set; }
    [Export] public Label?        AccuracyLabel   { get; set; }
    [Export] public ProgressBar?  HealthBar       { get; set; }
    [Export] public Label?        JudgmentDisplay { get; set; }

    private Tween? _judgmentTween;

    /// <summary>由 GameplayController 在 _Ready 时调用，订阅 ScoreTracker 信号</summary>
    public void ConnectToTracker(ScoreTracker tracker)
    {
        tracker.ScoreUpdated  += OnScoreUpdated;
        tracker.HealthChanged += OnHealthChanged;
    }

    public void ShowJudgment(Constants.Judgment judgment)
    {
        if (JudgmentDisplay is null) return;
        JudgmentDisplay.Text    = Constants.JudgmentDisplayText[judgment];
        JudgmentDisplay.Visible = true;

        _judgmentTween?.Kill();
        _judgmentTween = CreateTween();
        _judgmentTween.TweenProperty(JudgmentDisplay, "modulate:a", 0f, 0.4f)
            .SetDelay(0.3);
    }

    // ── 信号处理 ──────────────────────────────────────────────

    private void OnScoreUpdated(int score, int combo, double accuracy)
    {
        if (ScoreLabel   is not null) ScoreLabel.Text   = score.ToString("N0");
        if (ComboLabel   is not null) ComboLabel.Text   = combo > 0 ? $"{combo}" : "";
        if (AccuracyLabel is not null) AccuracyLabel.Text = MathUtils.FormatAccuracy(accuracy);
    }

    private void OnHealthChanged(float health)
    {
        if (HealthBar is not null) HealthBar.Value = health;
    }
}
