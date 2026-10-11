using System;
using UnityEngine;

/// <summary>
/// Quản lý điểm TỔNG của player local, gộp từ nhiều nguồn khác nhau
/// (quiz, coin, rank bonus, ...). Đây vẫn là "cửa ngõ" duy nhất chứa
/// totalScore và bắn event - mọi nguồn điểm đều phải đi qua AddScore(),
/// không có chỗ nào khác được cộng thẳng vào totalScore.
///
/// Observer Pattern: network code / UI subscribe OnScoreUpdated,
/// không cần biết điểm đến từ nguồn nào hay được tính ra sao.
///
/// MỞ RỘNG THÊM NGUỒN ĐIỂM MỚI (vd: Achievement):
/// 1. Thêm giá trị vào enum ScoreSource
/// 2. Tạo AchievementScoreRule : ScriptableObject, IScoreRule<TInputMoi>
/// 3. Thêm field [SerializeField] AchievementScoreRule achievementRule;
/// 4. Thêm hàm AddAchievementScore(...) gọi AddScore(...) như các hàm dưới
/// Không cần sửa các nguồn điểm đã có.
/// </summary>
/// 
public enum ScoreSource
{
    Quiz,
    Coin,
    Rank
    
}

public class PlayerScoreManager : MonoBehaviour
{
    public static PlayerScoreManager Instance { get; private set; }

    [Header("Score Rules (ScriptableObject assets)")]
    [SerializeField] private QuizScoreRule quizRule;

    [SerializeField] private RankBonusRule rankBonusRule;

    [SerializeField] private int totalScore = 0;
    [SerializeField] private int currentStreak = 0;

    public int TotalScore => totalScore;
    public int CurrentStreak => currentStreak;

    /// Bắn ra: (điểm vừa cộng, tổng điểm hiện tại, nguồn điểm)
    /// Network code / UI subscribe vào đây để đồng bộ hoặc hiển thị.
    public event Action<int, int, ScoreSource> OnScoreUpdated;

    public event Action<int> OnStreakChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ---------- Các entry point theo từng nguồn điểm ----------

    /// Gọi khi player trả lời xong 1 câu hỏi.
    public void AddQuizScore(AnswerResult result)
    {
        int previousStreak = currentStreak;
        currentStreak = result.IsCorrect ? currentStreak + 1 : 0;
        OnStreakChanged?.Invoke(currentStreak);

        int gained = quizRule.CalculateScore(new QuizAnswerInput(result, currentStreak));

        Debug.Log(
            $"[QuizScore] IsCorrect={result.IsCorrect} " +
            $"TimeTaken={result.TimeTaken:F2}s/{result.TimeLimit:F2}s " +
            $"Difficulty={result.Difficulty} " +
            $"Streak={previousStreak}->{currentStreak} " +
            $"Gained={gained}"
        );

        AddScore(gained, ScoreSource.Quiz);
    }

    /// Gọi khi player nhặt 1 coin.
    public void AddCoinScore(int coinvalue)
    {      
        AddScore(coinvalue, ScoreSource.Coin);
    }

    /// Gọi khi trận đấu kết thúc, theo thứ hạng về đích của player (1 = nhất).
    public void AddRankBonus(int finishRank)
    {
        int gained = rankBonusRule.CalculateScore(finishRank);
        AddScore(gained, ScoreSource.Rank);
    }

     //---------- Nơi tổng hợp điểm duy nhất ----------

    private void AddScore(int amount, ScoreSource source)
    {
        totalScore += amount;
        OnScoreUpdated?.Invoke(amount, totalScore, source);
    }

    public void ResetScore()
    {
        totalScore = 0;
        currentStreak = 0;
        OnScoreUpdated?.Invoke(0, totalScore, ScoreSource.Quiz);
        OnStreakChanged?.Invoke(currentStreak);
    }
}