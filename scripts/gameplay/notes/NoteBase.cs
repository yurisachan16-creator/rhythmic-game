using Godot;

namespace RhythmicGame;

/// <summary>所有音符节点的基类。</summary>
public abstract partial class NoteBase : Node2D
{
    protected NoteData? Data;
    protected double LeadTimeMs;

    /// <summary>由 NoteSpawner 调用，传入数据并初始化视觉状态</summary>
    public virtual void Initialize(NoteData data, double leadTimeMs)
    {
        Data       = data;
        LeadTimeMs = leadTimeMs;
        Visible    = true;
    }

    /// <summary>归还对象池前重置状态</summary>
    public virtual void Reset()
    {
        Data      = null;
        Position  = Vector2.Zero;
    }

    /// <summary>
    /// 每帧根据当前歌曲时间更新音符位置。
    /// 子类可重写实现特殊移动效果。
    /// </summary>
    public override void _Process(double delta)
    {
        if (Data is null) return;

        double nowMs = GetSongPositionMs();
        float progress = (float)((Data.TimeMs - nowMs) / LeadTimeMs); // 1.0=刚出现，0.0=到判定线
        UpdatePosition(progress);
    }

    protected virtual void UpdatePosition(float progress)
    {
        float screenH    = GetViewport().GetVisibleRect().Size.Y;
        float travelPx   = screenH - Constants.JudgmentLineYOffset;
        Position = new Vector2(Position.X, screenH - Constants.JudgmentLineYOffset - travelPx * progress);
    }

    protected double GetSongPositionMs()
    {
        var audio    = GetNode<AudioManager>("/root/AudioManager");
        var settings = GetNode<SettingsManager>("/root/SettingsManager");
        return audio.GetSongPosition() * 1000.0 + settings.Settings.GlobalOffset;
    }
}
