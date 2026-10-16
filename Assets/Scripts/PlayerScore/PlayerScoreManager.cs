using System;
using UnityEngine;

/// <summary>Nguồn của một lần cộng điểm.</summary>
public enum ScoreSource
{
    Quiz,
    Coin,
    Rank
}

/// <summary>
/// Điểm tổng của người chơi local, gộp từ nhiều nguồn (quiz, coin, thưởng hạng). Mọi nguồn phải đi qua AddScore().
/// Thêm nguồn mới: thêm giá trị vào ScoreSource, tạo một rule asset (IScoreRule) và một hàm Add...Score gọi AddScore().
/// </summary>
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

    /// Gọi khi player trả lời xong 1 câu hỏi.
    public void AddQuizScore(AnswerResult result)
    {
        currentStreak = result.IsCorrect ? currentStreak + 1 : 0;
        OnStreakChanged?.Invoke(currentStreak);

        int gained = quizRule.CalculateScore(new QuizAnswerInput(result, currentStreak));

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