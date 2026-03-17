using Godot;

namespace RhythmicGame;

/// <summary>谱面合法性检查，用于 UGC 谱面导入时的验证</summary>
public static class ChartValidator
{
    public record ValidationResult(bool IsValid, List<string> Errors);

    public static ValidationResult Validate(ChartData chart)
    {
        var errors = new List<string>();

        if (chart.KeyCount is < 1 or > 9)
            errors.Add($"key_count 超出范围：{chart.KeyCount}（允许 1~9）");

        if (chart.Od is < 0 or > 10)
            errors.Add($"od 超出范围：{chart.Od}（允许 0~10）");

        if (chart.BpmEvents.Count == 0)
            errors.Add("bpm_events 不能为空");
        else if (chart.BpmEvents[0].GetValueOrDefault("beat", -1.0).AsDouble() != 0.0)
            errors.Add("bpm_events 第一个事件必须在第0拍");

        foreach (var ev in chart.BpmEvents)
        {
            double bpm = ev.GetValueOrDefault("bpm", 0.0).AsDouble();
            if (bpm <= 0)
                errors.Add($"bpm_events 存在无效 BPM 值：{bpm}");
        }

        foreach (var note in chart.Notes)
        {
            if (note.Lane < 0 || note.Lane >= chart.KeyCount)
                errors.Add($"音符 lane 超出范围：{note.Lane}（谱面 key_count={chart.KeyCount}）");

            if (note.Beat < 0)
                errors.Add($"音符 beat 不能为负：{note.Beat}");

            if (note.Type != NoteData.NoteType.Tap && note.EndBeat <= note.Beat)
                errors.Add($"Hold/Slide 音符的 end_beat（{note.EndBeat}）必须大于 beat（{note.Beat}）");

            if (note.Type == NoteData.NoteType.Slide)
            {
                if (note.LaneEnd < 0 || note.LaneEnd >= chart.KeyCount)
                    errors.Add($"Slide 音符 lane_end 超出范围：{note.LaneEnd}");
                if (note.LaneEnd == note.Lane)
                    errors.Add($"Slide 音符 lane_end 与 lane 相同：{note.Lane}");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
