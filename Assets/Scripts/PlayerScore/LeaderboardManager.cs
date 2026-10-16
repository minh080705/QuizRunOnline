using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// NetworkObject duy nhất tổng hợp số liệu của mọi người chơi (điểm, xu, số lần chết, tên) trong một NetworkDictionary.
/// Các máy báo số liệu qua RPC, chỉ StateAuthority (Master) ghi, Fusion đồng bộ xuống các máy còn lại.
/// Mỗi người một struct nên các chỉ số luôn được cập nhật cùng lúc. Đặt sẵn trong RaceScene.
/// </summary>
public class LeaderboardManager : NetworkBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

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
    /// Gọi hàm này từ máy local mỗi khi số liệu của player
    /// TRÊN MÁY ĐÓ thay đổi. Bên trong sẽ tự gửi RPC lên StateAuthority.
    /// </summary>
    public void ReportStats(int totalScore, int coins, int deaths)
    {
        RPC_ReportStats(Runner.LocalPlayer, totalScore, coins, deaths, LocalDisplayName(Runner.LocalPlayer));
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportStats(PlayerRef player, int totalScore, int coins, int deaths, string displayName)
    {
        Entries.Set(player, new PlayerRaceEntry
        {
            Score = totalScore,
            Coins = coins,
            Deaths = deaths,
            Name = displayName,
        });
    }

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
/// <summary>
/// Danh sách theo điểm giảm dần; hòa điểm xếp theo PlayerId để thứ tự giống nhau trên mọi máy.
/// </summary>
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
