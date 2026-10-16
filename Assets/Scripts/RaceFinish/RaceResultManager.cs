using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

/// <summary>
/// Trọng tài xếp hạng của cả trận. Là scene object (có NetworkObject) tồn tại từ đầu trận, KHÔNG nằm trong
/// prefab vạch đích: vạch đích chỉ spawn khi người dẫn đầu tới gần, còn người chơi có thể bị loại (hết mạng)
/// từ trước đó, thậm chí tất cả đều bị loại mà không ai tới được đích.
///
/// Mỗi máy tự phát hiện sự kiện của ĐÚNG player do máy đó điều khiển (chạm vạch đích / hết mạng) rồi gửi RPC
/// cho StateAuthority. StateAuthority chỉ làm trọng tài:
/// - Người về đích: nhận hạng ngay theo thứ tự chạm vạch.
/// - Người bị loại: nhớ Z lúc bị loại; hạng chốt khi cả trận đã phân định (mọi người đã về đích hoặc bị loại):
///   xếp sau tất cả người về đích, trong nhóm bị loại ai đi xa hơn (gần đích hơn) thì hạng cao hơn.
/// - Tự gán hạng cuối cho người còn lại duy nhất (chỉ khi chưa ai bị loại), và xử lý khi có người rời phòng.
///
/// Đặt component này trên một GameObject trong RaceScene đã có NetworkObject (ví dụ object FinishLineSpawner).
/// </summary>
public class RaceResultManager : NetworkBehaviour, IPlayerLeft
{
    public static RaceResultManager Instance { get; private set; }

    // Capacity phải >= số người chơi tối đa game hỗ trợ.
    [Networked, Capacity(8)]
    private NetworkLinkedList<PlayerRef> FinishOrder => default;

    // Người bị loại -> Z lúc bị loại (để xếp hạng theo khoảng cách tới đích).
    [Networked, Capacity(8)]
    private NetworkDictionary<PlayerRef, float> EliminatedZ => default;

    [Networked] public int TotalPlayers { get; private set; }

    /// true khi mọi người đã về đích hoặc bị loại và hạng đã được chốt.
    [Networked] public NetworkBool IsRaceFinished { get; private set; }

    /// Bắn ra trên MỌI máy sau khi toàn bộ người chơi đã được phân định thứ hạng.
    public static event Action OnRaceFinish;

    public override void Spawned()
    {
        Instance = this;

        if (Object.HasStateAuthority && TotalPlayers == 0)
        {
            // Snapshot số người chơi lúc vào trận. Có người rời giữa chừng thì PlayerLeft() trừ đi.
            TotalPlayers = Runner.ActivePlayers.Count();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Player local vừa chạm vạch đích.</summary>
    public void ReportFinish()
    {
        RPC_ReportFinish();
    }

    /// <summary>Player local vừa hết mạng; z = vị trí Z lúc bị loại.</summary>
    public void ReportEliminated(float z)
    {
        RPC_ReportEliminated(z);
    }

    // Lấy người gửi từ chính RPC (info.Source) thay vì tin tham số client gửi,
    // nên không máy nào khai báo thay người khác được.

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportFinish(RpcInfo info = default)
    {
        TryFinishPlayer(info.Source);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportEliminated(float z, RpcInfo info = default)
    {
        TryEliminatePlayer(info.Source, z);
    }

    private void TryFinishPlayer(PlayerRef player)
    {
        if (IsRaceFinished) return;
        if (FinishOrder.Contains(player) || EliminatedZ.ContainsKey(player)) return;

        int rank = FinishOrder.Count + 1;
        FinishOrder.Add(player);

        RPC_NotifyPlayerFinished(player, rank);
        EvaluateRace();
    }

    private void TryEliminatePlayer(PlayerRef player, float z)
    {
        if (IsRaceFinished) return;
        if (FinishOrder.Contains(player) || EliminatedZ.ContainsKey(player)) return;

        EliminatedZ.Set(player, z);

        // Mọi máy cần biết để loại người này khỏi danh sách camera quan sát.
        RPC_NotifyPlayerEliminated(player);
        EvaluateRace();
    }

    /// <summary>
    /// Kiểm tra cả trận đã phân định chưa. exclude: người vừa rời phòng
    /// (phòng trường hợp họ còn nằm trong ActivePlayers).
    /// </summary>
    private void EvaluateRace(PlayerRef exclude = default)
    {
        if (IsRaceFinished) return;

        int resolved = FinishOrder.Count + EliminatedZ.Count;

        if (resolved >= TotalPlayers)
        {
            ResolveRace();
            return;
        }

        // Chỉ còn đúng 1 người chưa phân định VÀ chưa ai bị loại: hạng của họ chắc chắn là cuối bảng -> tự gán.
        // Nếu đã có người bị loại thì người cuối cùng vẫn phải về đích (hạng tốt hơn mọi người bị loại),
        // không được tự gán, nếu không họ được hạng cao miễn phí.
        if (TotalPlayers - resolved == 1 && EliminatedZ.Count == 0)
            AutoFinishLastPlayer(exclude);
    }

    private void AutoFinishLastPlayer(PlayerRef exclude)
    {
        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            if (p == exclude) continue;

            if (!FinishOrder.Contains(p) && !EliminatedZ.ContainsKey(p))
            {
                TryFinishPlayer(p);
                break;
            }
        }
    }

    /// <summary>Chốt hạng cho người bị loại rồi báo kết thúc trận.</summary>
    private void ResolveRace()
    {
        if (IsRaceFinished) return;
        IsRaceFinished = true;

        var eliminated = new List<KeyValuePair<PlayerRef, float>>();
        foreach (var kvp in EliminatedZ)
            eliminated.Add(kvp);

        // Z lớn hơn = gần đích hơn = hạng cao hơn. Bằng nhau thì theo PlayerId để kết quả luôn xác định.
        eliminated.Sort((a, b) =>
        {
            int byZ = b.Value.CompareTo(a.Value);
            return byZ != 0 ? byZ : a.Key.PlayerId.CompareTo(b.Key.PlayerId);
        });

        // Người bị loại xếp sau tất cả người đã về đích.
        int rank = FinishOrder.Count;
        foreach (var kvp in eliminated)
        {
            rank++;
            RPC_NotifyPlayerRanked(kvp.Key, rank);
        }

        RPC_NotifyRaceFinished();
    }

    /// <summary>
    /// Có người rời phòng khi chưa về đích / chưa bị loại -> không chờ họ nữa, tránh cuộc đua treo.
    /// </summary>
    public void PlayerLeft(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (IsRaceFinished) return;
        if (FinishOrder.Contains(player) || EliminatedZ.ContainsKey(player)) return; // đã phân định rồi

        TotalPlayers--;
        EvaluateRace(player);
    }

    /// <summary>
    /// Broadcast tới TẤT CẢ client: máy đang spectator cũng cần biết ai vừa
    /// về đích để loại họ khỏi danh sách theo dõi.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerFinished(PlayerRef player, int rank)
    {
        if (Runner.LocalPlayer == player)
        {
            PlayerScoreManager.Instance?.AddRankBonus(rank);

            Transform me = PlayerContext.Instance != null ? PlayerContext.Instance.LocalPlayer : null;
            if (me != null)
            {
                var handler = me.GetComponentInChildren<PlayerFinishHandler>();
                if (handler != null) handler.OnFinishedRace(rank);
            }
        }

        RaceSpectatorManager.Instance?.MarkPlayerFinished(player);
    }

    /// <summary>Người này đã hết mạng: không còn là mục tiêu để camera quan sát theo dõi.</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerEliminated(PlayerRef player)
    {
        RaceSpectatorManager.Instance?.MarkPlayerFinished(player);
    }

    /// <summary>Hạng chốt cuối trận của người bị loại: chỉ cộng điểm thưởng, không có chuỗi về đích.</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerRanked(PlayerRef player, int rank)
    {
        if (Runner.LocalPlayer == player)
            PlayerScoreManager.Instance?.AddRankBonus(rank);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyRaceFinished()
    {
        OnRaceFinish?.Invoke();
    }
}
