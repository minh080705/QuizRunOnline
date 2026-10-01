using UnityEngine;
[System.Serializable]
public struct AnswerResult
{
    public bool IsCorrect;

    /// Thời gian player mất để trả lời (giây)
    public float TimeTaken;

    /// Thời gian giới hạn cho câu hỏi này (giây)
    public float TimeLimit;

    /// Độ khó câu hỏi (1 = dễ, 2 = trung bình, 3 = khó...)
    public int Difficulty;

    public AnswerResult(bool isCorrect, float timeTaken, float timeLimit, int difficulty)
    {
        IsCorrect = isCorrect;
        TimeTaken = timeTaken;
        TimeLimit = timeLimit;
        Difficulty = Mathf.Max(1, difficulty);
    }
}
/// <summary>
/// Input cho việc tính điểm quiz: gộp AnswerResult + streak hiện tại
/// (streak thuộc về trạng thái player, không thuộc về câu hỏi,
/// nên không để trong AnswerResult).
/// </summary>
public struct QuizAnswerInput
{
    public AnswerResult Result;
    public int CurrentStreak;

    public QuizAnswerInput(AnswerResult result, int currentStreak)
    {
        Result = result;
        CurrentStreak = currentStreak;
    }
}

/// <summary>
/// Cấu hình tính điểm cho nguồn "trả lời câu hỏi".
/// Là ScriptableObject vì đây là bảng số liệu, designer chỉnh trong
/// Inspector, không cần gắn GameObject. Tạo asset qua menu:
/// Assets > Create > Score Rules > Quiz Score Rule
/// </summary>
[CreateAssetMenu(menuName = "Score Rules/Quiz Score Rule", fileName = "QuizScoreRule")]
public class QuizScoreRule : ScriptableObject, IScoreRule<QuizAnswerInput>
{
    [Header("Điểm cơ bản")]
    public int basePointsPerDifficulty = 10;

    [Header("Bonus tốc độ")]
    public int maxTimeBonus = 10;

    [Header("Bonus streak (trả lời đúng liên tiếp)")]
    public int streakBonusPerCombo = 5;
    public int maxStreakBonus = 50;

    public int CalculateScore(QuizAnswerInput input)
    {
        var result = input.Result;
        if (!result.IsCorrect)
            return 0;

        int baseScore = basePointsPerDifficulty * result.Difficulty;

        float timeLimit = Mathf.Max(0.01f, result.TimeLimit);
        float timeRatio = Mathf.Clamp01(1f - (result.TimeTaken / timeLimit));
        int timeBonus = Mathf.RoundToInt(timeRatio * maxTimeBonus);

        int streakBonus = Mathf.Min(input.CurrentStreak * streakBonusPerCombo, maxStreakBonus);

        return baseScore + timeBonus + streakBonus;
    }
}