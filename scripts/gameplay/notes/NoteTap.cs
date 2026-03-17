using Godot;

namespace RhythmicGame;

public partial class NoteTap : NoteBase
{
    // 子节点引用（在场景编辑器中设置）
    [Export] public ColorRect? Body         { get; set; }
    [Export] public CpuParticles2D? HitFx   { get; set; }

    public override void Initialize(NoteData data, double leadTimeMs)
    {
        base.Initialize(data, leadTimeMs);
        if (Body is not null)
            Body.Color = new Color(0.4f, 0.8f, 1.0f); // 默认蓝白色，后续走皮肤系统
    }

    public void PlayHitEffect()
    {
        HitFx?.Restart();
    }

    public override void Reset()
    {
        base.Reset();
        HitFx?.Stop();
    }
}
