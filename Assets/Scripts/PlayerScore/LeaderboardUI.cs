using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI DEMO hiển thị bảng xếp hạng - đọc từ LeaderboardManager.
/// Dùng cách đơn giản nhất cho demo: poll (kiểm tra lại) định kỳ mỗi
/// refreshInterval giây, KHÔNG dùng cơ chế change-detection phức tạp của
/// Fusion cho NetworkDictionary. Với leaderboard vài chục người, chi phí
/// polling này không đáng kể.
///
/// Dùng Object Pool đơn giản cho các dòng (row) để tránh Instantiate/Destroy
/// liên tục mỗi lần refresh - tái sử dụng lại các row đã tạo.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rowContainer; // Content của ScrollView
    [SerializeField] private LeaderboardRow rowPrefab;

    [Header("Settings")]
    [SerializeField] private float refreshInterval = 0.5f;

    private readonly List<LeaderboardRow> activeRows = new List<LeaderboardRow>();
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < refreshInterval)
            return;

        timer = 0f;
        Refresh();
    }

    private void Refresh()
    {
        if (LeaderboardManager.Instance == null)
            return;

        List<LeaderboardEntry> entries = LeaderboardManager.Instance.GetSortedScores();

        // Đảm bảo đủ số row cần thiết (tái sử dụng row cũ, tạo thêm nếu thiếu)
        while (activeRows.Count < entries.Count)
        {
            LeaderboardRow newRow = Instantiate(rowPrefab, rowContainer);
            activeRows.Add(newRow);
        }

        // Ẩn bớt row thừa nếu số người chơi giảm (ví dụ có người rời phòng)
        for (int i = 0; i < activeRows.Count; i++)
        {
            activeRows[i].gameObject.SetActive(i < entries.Count);
        }

        // Đổ dữ liệu vào từng row theo thứ hạng đã sắp xếp sẵn
        for (int i = 0; i < entries.Count; i++)
        {
            // Tên người chơi (lấy từ hồ sơ đăng nhập) do LeaderboardManager đồng bộ cùng điểm.
            string displayName = LeaderboardManager.Instance.GetName(entries[i].Player);
            activeRows[i].SetData(rank: i + 1, displayName: displayName, score: entries[i].Score);
        }
    }
}