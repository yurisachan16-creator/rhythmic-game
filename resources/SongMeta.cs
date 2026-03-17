using Godot;
using Godot.Collections;

namespace RhythmicGame;

[GlobalClass]
public partial class SongMeta : Resource
{
    [Export] public string Title { get; set; } = "";
    [Export] public string TitleUnicode { get; set; } = "";
    [Export] public string Artist { get; set; } = "";
    [Export] public string ArtistUnicode { get; set; } = "";
    [Export] public float Bpm { get; set; } = 120f;
    [Export] public float BpmMax { get; set; } = 0f;    // 变BPM时最高值，0=固定BPM
    [Export] public string AudioFile { get; set; } = "";
    [Export] public string CoverFile { get; set; } = "";
    [Export] public float PreviewStart { get; set; } = 0f;

    /// <summary>运行时填入：歌曲目录绝对路径</summary>
    public string SongDir { get; set; } = "";

    /// <summary>各难度信息，元素为 Dictionary { difficulty, file, level }</summary>
    [Export] public Array<Dictionary> Charts { get; set; } = [];

    public string GetDisplayTitle() =>
        TitleUnicode.Length > 0 ? TitleUnicode : Title;

    public string GetDisplayArtist() =>
        ArtistUnicode.Length > 0 ? ArtistUnicode : Artist;

    public string GetBpmDisplay() =>
        BpmMax > 0f ? $"{(int)Bpm}-{(int)BpmMax}" : ((int)Bpm).ToString();

    public Dictionary GetChartInfo(string difficulty)
    {
        foreach (Dictionary info in Charts)
            if (info.TryGetValue("difficulty", out var d) && d.AsString() == difficulty)
                return info;
        return [];
    }
}
