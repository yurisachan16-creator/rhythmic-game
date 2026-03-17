namespace RhythmicGame;

public static class MathUtils
{
    public static string GetGrade(int score)
    {
        foreach (var (grade, threshold) in Constants.GradeThresholds)
            if (score >= threshold)
                return grade;
        return "F";
    }

    public static string FormatDuration(float seconds)
    {
        int m = (int)seconds / 60;
        int s = (int)seconds % 60;
        return $"{m}:{s:D2}";
    }

    public static string FormatAccuracy(double acc) =>
        $"{acc * 100.0:F2}%";

    public static float Remap(float value,
        float inMin, float inMax, float outMin, float outMax) =>
        outMin + (value - inMin) / (inMax - inMin) * (outMax - outMin);
}
