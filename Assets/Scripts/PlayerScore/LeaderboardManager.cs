using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Network Object DUY NHẤT quản lý điểm của TOÀN BỘ người chơi.
/// - Giữ 1 NetworkDictionary (PlayerRef -> điểm tổng), Fusion TỰ ĐỘNG đồng bộ
///   giá trị này từ StateAuthority (host) xuống mọi client, không cần code
///   thêm để "gửi" xuống các máy khác.
/// - Chỉ StateAuthority được phép GHI vào dictionary (qua RPC), tránh xung
///   đột nếu nhiều máy cùng ghi 1 lúc.
/// - Đây là NetworkObject riêng biệt, KHÔNG phải PlayerScoreManager (vẫn giữ
///   nguyên là điểm local từng máy) - LeaderboardManager chỉ đứng ra
///   TỔNG HỢP điểm báo về từ các máy, không tự tính điểm.
///
/// Setup: tạo 1 Prefab rỗng, gắn NetworkObject + script này, Spawn 1 LẦN DUY
/// NHẤT bởi host lúc bắt đầu trận (Runner.Spawn), hoặc đặt sẵn làm Scene
/// Object nếu dùng Fusion Shared/Host mode với 1 scene cố định.
/// </summary>
public class LeaderboardManager : NetworkBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    // Capacity = số người chơi tối đa hỗ trợ. Chỉnh theo nhu cầu game.
    [Networked, Capacity(16)]
    public NetworkDictionary<PlayerRef, int> Scores => default;

    public override void Spawned()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Gọi hàm này từ máy local (client hoặc host) mỗi khi điểm của player
    /// TRÊN MÁY ĐÓ thay đổi. Bên trong sẽ tự gửi RPC lên StateAuthority.
    /// </summary>
    public void ReportScore(int totalScore)
    {
        RPC_ReportScore(Runner.LocalPlayer, totalScore);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportScore(PlayerRef player, int totalScore)
    {
        // Chỉ StateAuthority chạy dòng này -> ghi vào NetworkDictionary,
        // Fusion tự động replicate giá trị mới xuống mọi client sau đó.
        Scores.Set(player, totalScore);
    }

    /// <summary>
    /// Trả về danh sách điểm đã sắp xếp giảm dần, dùng để hiển thị UI.
    /// Có thể gọi từ bất kỳ máy nào (client lẫn host) vì Scores đã được
    /// Fusion đồng bộ sẵn.
    /// </summary>
    public List<LeaderboardEntry> GetSortedScores()
    {
        var result = new List<LeaderboardEntry>();

        foreach (var kvp in Scores)
        {
            result.Add(new LeaderboardEntry
            {
                Player = kvp.Key,
                Score = kvp.Value
            });
        }

        result.Sort((a, b) => b.Score.CompareTo(a.Score));
        return result;
    }
}

/// <summary>
/// 1 dòng dữ liệu trong bảng xếp hạng - tách riêng khỏi PlayerRef thô để
/// UI dễ dùng hơn (có thể mở rộng thêm Nickname sau này nếu có hệ thống tên).
/// </summary>
public struct LeaderboardEntry
{
    public PlayerRef Player;
    public int Score;
}