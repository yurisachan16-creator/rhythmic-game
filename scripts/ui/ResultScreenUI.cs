using Godot;

namespace RhythmicGame;

/// <summary>结算界面，从 GameManager 读取本次成绩并展示。</summary>
public partial class ResultScreenUI : Node
{
    [Export] public Label?  GradeLabel      { get; set; }
    [Export] public Label?  ScoreLabel      { get; set; }
    [Export] public Label?  AccuracyLabel   { get; set; }
    [Export] public Label?  MaxComboLabel   { get; set; }
    [Export] public Label?  MaxPerfectLabel { get; set; }
    [Export] public Label?  PerfectLabel    { get; set; }
    [Export] public Label?  GreatLabel      { get; set; }
    [Export] public Label?  GoodLabel       { get; set; }
    [Export] public Label?  BadLabel        { get; set; }
    [Export] public Label?  MissLabel       { get; set; }
    [Export] public Label?  BadgeLabel      { get; set; }  // AP / FC 徽章
    [Export] public Label?  NewRecordLabel  { get; set; }
    [Export] public Button? RetryButton     { get; set; }
    [Export] public Button? BackButton      { get; set; }

    // 结算数据由 GameplayController 在结算时写入 GameManager，此处读取
    // 目前通过静态字段传递，后续可改为 Resource 或信号
    public static ScoreTracker? LastTracker { get; set; }

    private GameManager? _game;

    public override void _Ready()
    {
        _game = GetNode<GameManager>("/root/GameManager");

        if (LastTracker is not null)
            PopulateUI(LastTracker);

        RetryButton?.Pressed.Connect(OnRetry);  // TODO
        BackButton?.Pressed.Connect(OnBack);
    }

    private void PopulateUI(ScoreTracker tracker)
    {
        if (GradeLabel     is not null) GradeLabel.Text     = MathUtils.GetGrade(tracker.Score);
        if (ScoreLabel     is not null) ScoreLabel.Text     = tracker.Score.ToString("N0");
        if (AccuracyLabel  is not null) AccuracyLabel.Text  = MathUtils.FormatAccuracy(tracker.Accuracy);
        if (MaxComboLabel  is not null) MaxComboLabel.Text  = tracker.MaxCombo.ToString();
        if (MaxPerfectLabel is not null) MaxPerfectLabel.Text = tracker.CountMaxPerfect.ToString();
        if (PerfectLabel   is not null) PerfectLabel.Text   = tracker.CountPerfect.ToString();
        if (GreatLabel     is not null) GreatLabel.Text     = tracker.CountGreat.ToString();
        if (GoodLabel      is not null) GoodLabel.Text      = tracker.CountGood.ToString();
        if (BadLabel       is not null) BadLabel.Text       = tracker.CountBad.ToString();
        if (MissLabel      is not null) MissLabel.Text      = tracker.CountMiss.ToString();

        if (BadgeLabel is not null)
            BadgeLabel.Text = tracker.IsAllPerfect ? "AP" : tracker.IsFullCombo ? "FC" : "";
    }

    private void OnRetry()
    {
        // TODO: 直接重开上一首
    }

    private void OnBack() => _game?.GoToSongSelect();
}
