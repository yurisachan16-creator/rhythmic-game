using Godot;

namespace RhythmicGame;

/// <summary>单条轨道节点，负责视觉呈现和按键反馈特效。</summary>
public partial class Lane : Node2D
{
    [Export] public ColorRect?       Background    { get; set; }
    [Export] public Node2D?          NoteContainer { get; set; }
    [Export] public Line2D?          JudgmentLine  { get; set; }
    [Export] public CpuParticles2D?  HitEffect     { get; set; }

    /// <summary>按下时触发视觉反馈</summary>
    public void OnPressed()
    {
        HitEffect?.Restart();
        if (Background is not null)
            Background.Color = new Color(1f, 1f, 1f, 0.15f);
    }

    /// <summary>松开时恢复</summary>
    public void OnReleased()
    {
        if (Background is not null)
            Background.Color = new Color(1f, 1f, 1f, 0.05f);
    }

    /// <summary>短暂高亮（谱面事件用）</summary>
    public void FlashHighlight(Color color, float duration)
    {
        if (Background is null) return;
        var tween = CreateTween();
        tween.TweenProperty(Background, "color", color, 0.05f);
        tween.TweenProperty(Background, "color", new Color(1f, 1f, 1f, 0.05f), duration);
    }
}
