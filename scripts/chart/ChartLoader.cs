using Godot;
using Godot.Collections;
using System.Linq;

namespace RhythmicGame;

/// <summary>从文件系统读取 meta.json / chart.json，解析为 SongMeta / ChartData</summary>
public static class ChartLoader
{
    /// <summary>扫描指定目录，返回所有有效的 SongMeta 列表</summary>
    public static List<SongMeta> ScanSongs(string baseDir)
    {
        var results = new List<SongMeta>();
        using var dir = DirAccess.Open(baseDir);
        if (dir is null)
        {
            GD.PushError($"ChartLoader: 无法打开目录 {baseDir}");
            return results;
        }

        dir.ListDirBegin();
        string folderName = dir.GetNext();
        while (folderName != "")
        {
            if (dir.CurrentIsDir() && !folderName.StartsWith('.'))
            {
                string songDir = baseDir.PathJoin(folderName);
                var meta = LoadMeta(songDir);
                if (meta is not null)
                    results.Add(meta);
            }
            folderName = dir.GetNext();
        }
        dir.ListDirEnd();

        return results;
    }

    /// <summary>从歌曲目录加载 meta.json，失败返回 null</summary>
    public static SongMeta? LoadMeta(string songDir)
    {
        string metaPath = songDir.PathJoin("meta.json");
        string text = ReadFile(metaPath);
        if (text.Length == 0) return null;

        var parsed = Json.ParseString(text);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            GD.PushError($"ChartLoader: meta.json 格式错误 - {metaPath}");
            return null;
        }

        var dict = parsed.AsGodotDictionary();
        var meta = new SongMeta
        {
            Title         = dict.GetValueOrDefault("title", "Unknown").AsString(),
            TitleUnicode  = dict.GetValueOrDefault("title_unicode", "").AsString(),
            Artist        = dict.GetValueOrDefault("artist", "Unknown").AsString(),
            ArtistUnicode = dict.GetValueOrDefault("artist_unicode", "").AsString(),
            Bpm           = dict.GetValueOrDefault("bpm", 120).AsSingle(),
            AudioFile     = dict.GetValueOrDefault("audio_file", "").AsString(),
            CoverFile     = dict.GetValueOrDefault("cover_file", "").AsString(),
            PreviewStart  = dict.GetValueOrDefault("preview_start", 0).AsSingle(),
            SongDir       = songDir,
        };

        if (dict.ContainsKey("charts"))
            foreach (var item in dict["charts"].AsGodotArray())
                meta.Charts.Add(item.AsGodotDictionary());

        return meta;
    }

    /// <summary>加载并解析单个难度的谱面文件，失败返回 null</summary>
    public static ChartData? LoadChart(string songDir, string chartFile)
    {
        string chartPath = songDir.PathJoin(chartFile);
        string text = ReadFile(chartPath);
        if (text.Length == 0) return null;

        var parsed = Json.ParseString(text);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            GD.PushError($"ChartLoader: chart.json 格式错误 - {chartPath}");
            return null;
        }

        var dict  = parsed.AsGodotDictionary();
        var chart = new ChartData
        {
            Version    = dict.GetValueOrDefault("version", 1).AsInt32(),
            KeyCount   = dict.GetValueOrDefault("key_count", 4).AsInt32(),
            Od         = dict.GetValueOrDefault("od", 8).AsSingle(),
            SourcePath = chartPath,
        };

        if (dict.ContainsKey("bpm_events"))
            foreach (var item in dict["bpm_events"].AsGodotArray())
                chart.BpmEvents.Add(item.AsGodotDictionary());

        if (dict.ContainsKey("scroll_events"))
            foreach (var item in dict["scroll_events"].AsGodotArray())
                chart.ScrollEvents.Add(item.AsGodotDictionary());

        if (dict.ContainsKey("events"))
            foreach (var item in dict["events"].AsGodotArray())
                chart.Events.Add(item.AsGodotDictionary());

        if (dict.ContainsKey("notes"))
            foreach (var item in dict["notes"].AsGodotArray())
            {
                var note = ParseNote(item.AsGodotDictionary());
                if (note is not null)
                    chart.Notes.Add(note);
            }

        if (chart.BpmEvents.Count == 0 || chart.BpmEvents[0].GetValueOrDefault("beat", -1).AsDouble() != 0.0)
            GD.PushWarning($"ChartLoader: bpm_events 缺少第0拍事件 - {chartPath}");

        BeatCalculator.CalculateNoteTimes(chart);
        chart.TotalNotes = CountNoteEquivalents(chart.Notes);
        chart.DurationMs = BeatCalculator.CalculateDuration(chart);

        // 按 TimeMs 升序排序
        var sorted = chart.Notes.OrderBy(n => n.TimeMs).ToList();
        chart.Notes.Clear();
        foreach (var n in sorted) chart.Notes.Add(n);

        return chart;
    }

    // ── 私有方法 ──────────────────────────────────────────────────

    private static NoteData? ParseNote(Godot.Collections.Dictionary dict)
    {
        var note = new NoteData();
        string typeStr = dict.GetValueOrDefault("type", "TAP").AsString();
        note.Type = typeStr switch
        {
            "TAP"   => NoteData.NoteType.Tap,
            "HOLD"  => NoteData.NoteType.Hold,
            "SLIDE" => NoteData.NoteType.Slide,
            _ => (NoteData.NoteType)(-1),
        };

        if ((int)note.Type == -1)
        {
            GD.PushWarning($"ChartLoader: 未知音符类型 {typeStr}，跳过");
            return null;
        }

        note.Lane    = dict.GetValueOrDefault("lane", 0).AsInt32();
        note.Beat    = dict.GetValueOrDefault("beat", 0).AsDouble();
        note.EndBeat = dict.GetValueOrDefault("end_beat", 0).AsDouble();
        note.LaneEnd = dict.GetValueOrDefault("lane_end", -1).AsInt32();
        return note;
    }

    private static int CountNoteEquivalents(Godot.Collections.Array<NoteData> notes)
    {
        int total = 0;
        foreach (var note in notes)
            total += note.Type == NoteData.NoteType.Tap ? 1 : 2;
        return total;
    }

    private static string ReadFile(string path)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            GD.PushError($"ChartLoader: 文件不存在 - {path}");
            return "";
        }
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushError($"ChartLoader: 无法打开文件 - {path}");
            return "";
        }
        return file.GetAsText();
    }
}
