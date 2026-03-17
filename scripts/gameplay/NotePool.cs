using Godot;

namespace RhythmicGame;

/// <summary>
/// 音符对象池。避免频繁实例化/销毁节点。
/// 挂载到 GameplayScene，由 NoteSpawner 调用。
/// </summary>
public partial class NotePool : Node
{
    [Export] public PackedScene TapScene   { get; set; } = null!;
    [Export] public PackedScene HoldScene  { get; set; } = null!;

    private readonly Queue<NoteBase> _tapPool  = [];
    private readonly Queue<NoteBase> _holdPool = [];

    private const int InitialPoolSize = 16;

    public override void _Ready()
    {
        // 预热对象池
        for (int i = 0; i < InitialPoolSize; i++)
        {
            Prewarm(TapScene,  _tapPool);
            Prewarm(HoldScene, _holdPool);
        }
    }

    public NoteBase GetNote(NoteData.NoteType type)
    {
        var pool  = GetPool(type);
        var scene = GetScene(type);

        NoteBase note;
        if (pool.Count > 0)
        {
            note = pool.Dequeue();
        }
        else
        {
            note = scene.Instantiate<NoteBase>();
            AddChild(note);
        }

        note.Visible = true;
        return note;
    }

    public void ReleaseNote(NoteBase note, NoteData.NoteType type)
    {
        note.Visible = false;
        note.Reset();
        GetPool(type).Enqueue(note);
    }

    // ── 私有 ──────────────────────────────────────────────────

    private Queue<NoteBase> GetPool(NoteData.NoteType type) => type switch
    {
        NoteData.NoteType.Hold  => _holdPool,
        NoteData.NoteType.Slide => _holdPool, // Slide 复用 Hold 外观
        _                        => _tapPool,
    };

    private PackedScene GetScene(NoteData.NoteType type) => type switch
    {
        NoteData.NoteType.Hold  => HoldScene,
        NoteData.NoteType.Slide => HoldScene,
        _                        => TapScene,
    };

    private void Prewarm(PackedScene scene, Queue<NoteBase> pool)
    {
        var note = scene.Instantiate<NoteBase>();
        note.Visible = false;
        AddChild(note);
        pool.Enqueue(note);
    }
}
