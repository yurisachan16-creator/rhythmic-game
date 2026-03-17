using Godot.Collections;

namespace RhythmicGame;

public static class BeatCalculator
{
    /// <summary>
    /// 将谱面中所有音符的 Beat 转换为 TimeMs。
    /// 调用时机：ChartLoader 解析完 JSON 之后立即调用。
    /// </summary>
    public static void CalculateNoteTimes(ChartData chart)
    {
        foreach (var note in chart.Notes)
        {
            note.TimeMs = BeatToMs(note.Beat, chart.BpmEvents);
            if (note.Type is NoteData.NoteType.Hold or NoteData.NoteType.Slide)
                note.EndTimeMs = BeatToMs(note.EndBeat, chart.BpmEvents);
        }
    }

    /// <summary>
    /// 将拍位（beat）转换为毫秒时间。
    /// bpmEvents 必须按 beat 升序排列。
    /// </summary>
    public static double BeatToMs(double targetBeat, Array<Dictionary> bpmEvents)
    {
        if (bpmEvents.Count == 0)
        {
            GD.PushError("BeatCalculator: bpmEvents 为空");
            return 0.0;
        }

        double elapsedMs = 0.0;
        double currentBeat = 0.0;

        for (int i = 0; i < bpmEvents.Count; i++)
        {
            var ev = bpmEvents[i];
            double eventBeat = ev["beat"].AsDouble();
            double bpm       = ev["bpm"].AsDouble();
            double msPerBeat = 60_000.0 / bpm;

            double nextEventBeat = i + 1 < bpmEvents.Count
                ? bpmEvents[i + 1]["beat"].AsDouble()
                : double.PositiveInfinity;

            if (targetBeat <= nextEventBeat)
            {
                elapsedMs += (targetBeat - currentBeat) * msPerBeat;
                return elapsedMs;
            }

            elapsedMs   += (nextEventBeat - currentBeat) * msPerBeat;
            currentBeat  = nextEventBeat;
        }

        // 超出最后一个 BPM 事件，用最后的 BPM 继续
        double lastBpm = bpmEvents[^1]["bpm"].AsDouble();
        elapsedMs += (targetBeat - currentBeat) * (60_000.0 / lastBpm);
        return elapsedMs;
    }

    /// <summary>计算谱面总时长（毫秒），取最后一个音符的结束时间</summary>
    public static double CalculateDuration(ChartData chart)
    {
        double maxMs = 0.0;
        foreach (var note in chart.Notes)
        {
            double end = note.EndTimeMs > 0.0 ? note.EndTimeMs : note.TimeMs;
            if (end > maxMs) maxMs = end;
        }
        return maxMs;
    }
}
