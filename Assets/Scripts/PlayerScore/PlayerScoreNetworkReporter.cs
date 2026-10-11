using System.Collections;
using UnityEngine;

/// <summary>
/// Cầu nối giữa các nguồn số liệu LOCAL (PlayerScoreManager, CoinCaculator, PlayerLives - không biết gì về
/// network) và LeaderboardManager (network, tổng hợp số liệu của mọi người). Đặt script này
/// TRÊN CÙNG OBJECT hoặc 1 object riêng trong scene của LOCAL PLAYER.
///
/// Mỗi lần báo gom đủ 3 số: tổng điểm, số xu đã nhặt, số lần chết (cùng tên người chơi do LeaderboardManager gắn).
///
/// Vì sao tách riêng thay vì gọi thẳng trong PlayerScoreManager.AddScore():
/// PlayerScoreManager cần dùng được cả khi KHÔNG có network (test solo, hoặc
/// tái sử dụng cho single-player mode) - giữ nó không phụ thuộc Fusion.
/// </summary>
public class PlayerScoreNetworkReporter : MonoBehaviour
{
    private void Start()
    {
        // Dùng Start() thay vì OnEnable() - đảm bảo PlayerScoreManager.Awake()
        // và LeaderboardManager.Spawned() đã chạy xong trước đó (xem lý do
        // chi tiết đã giải thích ở AddPointsNotification.cs).
        if (PlayerScoreManager.Instance != null)
        {
            // Mọi người bắt đầu trận với 0 điểm, rồi báo 0 điểm đó lên bảng điểm ngay khi có thể -
            // nhờ vậy ai cũng có mặt trong bảng xếp hạng từ đầu (kể cả người chưa ghi điểm nào).
            PlayerScoreManager.Instance.ResetScore();
            PlayerScoreManager.Instance.OnScoreUpdated += HandleScoreUpdated;

            // Số lần chết đổi (không kèm đổi điểm) và lần báo chốt lúc hết trận (sau khi đã cộng điểm thưởng hạng).
            PlayerLives.OnLocalLivesChanged += Report;
            RaceResultManager.OnRaceFinish += Report;

            StartCoroutine(ReportInitialScore());
        }
        else
        {
            Debug.LogError("[PlayerScoreNetworkReporter] Không tìm thấy PlayerScoreManager.Instance.");
        }
    }

    // LeaderboardManager là scene NetworkObject nên có thể chưa spawn xong ở Start(): đợi tới khi có rồi mới báo.
    private IEnumerator ReportInitialScore()
    {
        yield return new WaitUntil(() => LeaderboardManager.Instance != null);
        Report();
    }

    private void OnDisable()
    {
        if (PlayerScoreManager.Instance != null)
            PlayerScoreManager.Instance.OnScoreUpdated -= HandleScoreUpdated;

        PlayerLives.OnLocalLivesChanged -= Report;
        RaceResultManager.OnRaceFinish -= Report;
    }

    private void HandleScoreUpdated(int gained, int total, ScoreSource source)
    {
        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("[PlayerScoreNetworkReporter] LeaderboardManager chưa sẵn sàng, bỏ qua lần báo điểm này.");
            return;
        }

        Report();
    }

    /// <summary>Gom điểm + xu + số lần chết của người chơi local rồi báo lên bảng chung.</summary>
    private void Report()
    {
        if (LeaderboardManager.Instance == null || PlayerScoreManager.Instance == null) return;

        int coins = CoinCaculator.Instance != null ? CoinCaculator.Instance.GetTotalCoin() : 0;

        PlayerLives lives = PlayerLives.Local;
        int deaths = lives != null ? lives.Deaths : 0;

        LeaderboardManager.Instance.ReportStats(PlayerScoreManager.Instance.TotalScore, coins, deaths);
    }
}
