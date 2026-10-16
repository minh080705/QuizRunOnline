using System.Collections.Generic;
using UnityEngine;

/// <summary>Hiển thị bảng xếp hạng từ LeaderboardManager: làm mới định kỳ mỗi refreshInterval giây và tái sử dụng các dòng đã tạo.</summary>
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

        while (activeRows.Count < entries.Count)
        {
            LeaderboardRow newRow = Instantiate(rowPrefab, rowContainer);
            activeRows.Add(newRow);
        }

        for (int i = 0; i < activeRows.Count; i++)
        {
            activeRows[i].gameObject.SetActive(i < entries.Count);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            // Tên người chơi (lấy từ hồ sơ đăng nhập) do LeaderboardManager đồng bộ cùng điểm.
            string displayName = LeaderboardManager.Instance.GetName(entries[i].Player);
            activeRows[i].SetData(rank: i + 1, displayName: displayName, score: entries[i].Score);
        }
    }
}