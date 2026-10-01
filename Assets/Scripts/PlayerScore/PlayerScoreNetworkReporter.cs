using UnityEngine;

/// <summary>
/// Cầu nối giữa PlayerScoreManager (local, không biết gì về network) và
/// LeaderboardManager (network, tổng hợp điểm mọi người). Đặt script này
/// TRÊN CÙNG OBJECT hoặc 1 object riêng trong scene của LOCAL PLAYER.
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
            PlayerScoreManager.Instance.OnScoreUpdated += HandleScoreUpdated;
        }
        else
        {
            Debug.LogError("[PlayerScoreNetworkReporter] Không tìm thấy PlayerScoreManager.Instance.");
        }
    }

    private void OnDisable()
    {
        if (PlayerScoreManager.Instance != null)
            PlayerScoreManager.Instance.OnScoreUpdated -= HandleScoreUpdated;
    }

    private void HandleScoreUpdated(int gained, int total, ScoreSource source)
    {
        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("[PlayerScoreNetworkReporter] LeaderboardManager chưa sẵn sàng, bỏ qua lần báo điểm này.");
            return;
        }

        LeaderboardManager.Instance.ReportScore(total);
    }
}