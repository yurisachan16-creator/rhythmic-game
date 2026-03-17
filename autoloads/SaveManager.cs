using Godot;
using System.Text.Json;

namespace RhythmicGame;

/// <summary>本地存档读写（user:// 目录）。注册为 Autoload。</summary>
public partial class SaveManager : Node
{
    // 内存缓存
    private Dictionary<string, RecordEntry> _records     = [];
    private HashSet<string>                 _achievements = [];

    public override void _Ready()
    {
        EnsureSaveDir();
        LoadAll();
    }

    // ── 游玩记录 ──────────────────────────────────────────────

    public record RecordEntry(
        int    Score,
        double Accuracy,
        int    MaxCombo,
        string Grade,
        bool   IsFullCombo,
        bool   IsAllPerfect,
        long   Timestamp
    );

    /// <summary>获取指定谱面的最佳记录，无记录时返回 null</summary>
    public RecordEntry? GetBestRecord(string chartId) =>
        _records.TryGetValue(chartId, out var r) ? r : null;

    /// <summary>保存记录（仅当优于历史最佳时更新）</summary>
    public bool TrySaveRecord(string chartId, RecordEntry entry)
    {
        if (_records.TryGetValue(chartId, out var existing) && existing.Score >= entry.Score)
            return false;

        _records[chartId] = entry;
        SaveRecords();
        return true;
    }

    // ── 成就 ──────────────────────────────────────────────────

    public bool IsAchievementUnlocked(string achId) => _achievements.Contains(achId);

    /// <summary>解锁成就，若已解锁则返回 false</summary>
    public bool UnlockAchievement(string achId)
    {
        if (!_achievements.Add(achId)) return false;
        SaveAchievements();
        return true;
    }

    // ── 持久化 ────────────────────────────────────────────────

    public void LoadAll()
    {
        _records     = LoadJson<Dictionary<string, RecordEntry>>(Constants.RecordsPath)     ?? [];
        _achievements = LoadJson<HashSet<string>>(Constants.AchievementsPath) ?? [];
    }

    private void SaveRecords()     => SaveJson(Constants.RecordsPath,     _records);
    private void SaveAchievements() => SaveJson(Constants.AchievementsPath, _achievements);

    private static void EnsureSaveDir()
    {
        if (!DirAccess.DirExistsAbsolute(Constants.SaveDir))
            DirAccess.MakeDirRecursiveAbsolute(Constants.SaveDir);
    }

    private static void SaveJson<T>(string path, T data)
    {
        string absPath = ProjectSettings.GlobalizePath(path);
        string json = JsonSerializer.Serialize(data,
            new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(absPath, json);
    }

    private static T? LoadJson<T>(string path)
    {
        string absPath = ProjectSettings.GlobalizePath(path);
        if (!System.IO.File.Exists(absPath)) return default;
        try
        {
            string json = System.IO.File.ReadAllText(absPath);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception e)
        {
            GD.PushError($"SaveManager: 读取存档失败 {path} - {e.Message}");
            return default;
        }
    }
}
