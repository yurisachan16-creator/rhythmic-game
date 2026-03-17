using Godot;

namespace RhythmicGame;

public partial class NoteHold : NoteBase
{
    [Export] public ColorRect? Head { get; set; }
    [Export] public ColorRect? Body { get; set; }
    [Export] public ColorRect? Tail { get; set; }

    private double _holdLengthMs = 0.0;

    public override void Initialize(NoteData data, double leadTimeMs)
    {
        base.Initialize(data, leadTimeMs);
        _holdLengthMs = data.EndTimeMs - data.TimeMs;
        UpdateBodyHeight();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Data is null) return;

        // Hold 持续中：头部已过判定线，Body 随时间缩短
        if (Data.IsHoldActive)
        {
            double nowMs     = GetSongPositionMs();
            double remaining = Data.EndTimeMs - nowMs;
            float  progress  = (float)(remaining / LeadTimeMs);
            float  screenH   = GetViewport().GetVisibleRect().Size.Y;
            float  travelPx  = screenH - Constants.JudgmentLineYOffset;

            if (Body is not null)
                Body.Size = new Vector2(Body.Size.X, Mathf.Max(0, travelPx * progress));
        }
    }

    private void UpdateBodyHeight()
    {
        if (Body is null) return;
        float screenH  = GetViewport().GetVisibleRect().Size.Y;
        float travelPx = screenH - Constants.JudgmentLineYOffset;
        float ratio    = (float)(_holdLengthMs / LeadTimeMs);
        Body.Size      = new Vector2(Body.Size.X, travelPx * ratio);
    }

    public override void Reset()
    {
        base.Reset();
        _holdLengthMs = 0.0;
        if (Body is not null)
            Body.Size = new Vector2(Body.Size.X, 0);
    }
}
