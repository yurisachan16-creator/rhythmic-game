using Godot;

namespace RhythmicGame;

/// <summary>
/// N轨抽象输入处理器。
/// 从 SettingsManager 读取键位配置，将具体按键转换为"轨道事件"信号。
/// 挂载到 GameplayScene 节点上。
/// </summary>
public partial class InputHandler : Node
{
    public event Action<int>? LanePressed;
    public event Action<int>? LaneReleased;

    private int _keyCount = 4;

    public override void _Ready()
    {
        _keyCount = GetNode<GameManager>("/root/GameManager")
            .CurrentSongMeta is not null
                ? 4  // TODO: 从 ChartData.KeyCount 读取
                : 4;
    }

    public override void _Input(InputEvent @event)
    {
        for (int lane = 0; lane < _keyCount; lane++)
        {
            string primaryAction = $"lane_{lane}_primary";

            if (InputMap.HasAction(primaryAction))
            {
                if (@event.IsActionPressed(primaryAction, false))
                    LanePressed?.Invoke(lane);
                else if (@event.IsActionReleased(primaryAction))
                    LaneReleased?.Invoke(lane);
            }
        }
    }

    public void SetKeyCount(int count) => _keyCount = count;
}
