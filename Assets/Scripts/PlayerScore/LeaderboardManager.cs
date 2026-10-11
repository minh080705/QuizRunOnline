using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Network Object DUY NHẤT quản lý số liệu của TOÀN BỘ người chơi (điểm, xu, số lần chết, tên).
/// - Giữ 1 NetworkDictionary (PlayerRef -> PlayerRaceEntry), Fusion TỰ ĐỘNG đồng bộ
///   giá trị này từ StateAuthority (host) xuống mọi client, không cần code
///   thêm để "gửi" xuống các máy khác.
/// - Chỉ StateAuthority được phép GHI vào dictionary (qua RPC), tránh xung
///   đột nếu nhiều máy cùng ghi 1 lúc.
/// - Đây là NetworkObject riêng biệt, KHÔNG phải PlayerScoreManager (vẫn giữ
///   nguyên là điểm local từng máy) - LeaderboardManager chỉ đứng ra
///   TỔNG HỢP số liệu báo về từ các máy, không tự tính gì.
/// - Mọi chỉ số của một người nằm chung 1 struct nên được cập nhật cùng lúc (nguyên tử):
///   bảng xếp hạng và bảng tổng kết trận không bao giờ thấy điểm mới đi kèm xu cũ.
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
    private NetworkDictionary<PlayerRef, PlayerRaceEntry> Entries => default;

    public override void Spawned()
    {
        Instance = this;
    }

    private static string LocalDisplayName(PlayerRef me)
    {
        UserProfile profile = UserProfileService.Instance != null ? UserProfileService.Instance.Current : null;
        string name = profile != null ? profile.characterName : null;
        if (string.IsNullOrEmpty(name)) name = $"Player {me.PlayerId}";
        return name.Length > 16 ? name.Substring(0, 16) : name; // khớp giới hạn tên ở lobby
    }

    public string GetName(PlayerRef player)
    {
        return Entries.TryGet(player, out var entry) ? entry.Name.ToString() : $"Player {player.PlayerId}";
    }

    /// <summary>Tên đã được người chơi này báo lên bảng chung; false nếu họ chưa báo lần nào.</summary>
    public bool TryGetName(PlayerRef player, out string name)
    {
        if (Entries.TryGet(player, out var entry))
        {
            name = entry.Name.ToString();
            return true;
        }

        name = null;
        return false;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Gọi hàm này từ máy local (client hoặc host) mỗi khi số liệu của player
    /// TRÊN MÁY ĐÓ thay đổi. Bên trong sẽ tự gửi RPC lên StateAuthority.
    /// </summary>
    public void ReportStats(int totalScore, int coins, int deaths)
    {
        RPC_ReportStats(Runner.LocalPlayer, totalScore, coins, deaths, LocalDisplayName(Runner.LocalPlayer));
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportStats(PlayerRef player, int totalScore, int coins, int deaths, string displayName)
    {
        // Chỉ StateAuthority chạy dòng này -> ghi vào NetworkDictionary,
        // Fusion tự động replicate giá trị mới xuống mọi client sau đó.
        Entries.Set(player, new PlayerRaceEntry
        {
            Score = totalScore,
            Coins = coins,
            Deaths = deaths,
            Name = displayName,
        });
    }

    /// <summary>
    /// Trả về danh sách đã sắp xếp theo điểm giảm dần, dùng để hiển thị UI.
    /// Hòa điểm thì theo PlayerId để thứ tự luôn giống nhau trên mọi máy.
    /// Có thể gọi từ bất kỳ máy nào (client lẫn host) vì Entries đã được
    /// Fusion đồng bộ sẵn.
    /// </summary>
    public List<LeaderboardEntry> GetSortedScores()
    {
        var result = new List<LeaderboardEntry>();

        foreach (var kvp in Entries)
        {
            result.Add(new LeaderboardEntry
            {
                Player = kvp.Key,
                Score = kvp.Value.Score,
                Coins = kvp.Value.Coins,
                Deaths = kvp.Value.Deaths,
                Name = kvp.Value.Name.ToString(),
            });
        }

        result.Sort((a, b) =>
        {
            int byScore = b.Score.CompareTo(a.Score);
            return byScore != 0 ? byScore : a.Player.PlayerId.CompareTo(b.Player.PlayerId);
        });
        return result;
    }
}

/// <summary>Số liệu của 1 người chơi, nằm trong NetworkDictionary của LeaderboardManager.</summary>
public struct PlayerRaceEntry : INetworkStruct
{
    public int Score;
    public int Coins;
    public int Deaths;
    public NetworkString<_16> Name;
}

/// <summary>
/// 1 dòng dữ liệu trong bảng xếp hạng / bảng tổng kết - tách riêng khỏi PlayerRef thô để UI dễ dùng hơn.
/// </summary>
public struct LeaderboardEntry
{
    public PlayerRef Player;
    public int Score;
    public int Coins;
    public int Deaths;
    public string Name;
}
