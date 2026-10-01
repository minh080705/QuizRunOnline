using System;
using System.Linq;
using Fusion;
using UnityEngine;

/// <summary>
/// NetworkObject đặt tại vạch đích. Mỗi máy tự phát hiện va chạm của ĐÚNG
/// player do máy đó điều khiển, rồi gửi RPC báo cho StateAuthority.
/// StateAuthority chỉ làm trọng tài: xử lý báo cáo theo thứ tự đến nơi.
/// Tự gán hạng cuối cho người còn lại duy nhất, và xử lý khi có người rời phòng.
/// </summary>
public class FinishLine : NetworkBehaviour, IPlayerLeft
{
    // Capacity phải >= số người chơi tối đa game hỗ trợ.
    [Networked, Capacity(8)]
    private NetworkLinkedList<PlayerRef> FinishOrder => default;

    [Networked] public int TotalPlayers { get; private set; }

    /// Bắn ra trên MỌI máy sau khi toàn bộ người chơi đã được gán thứ hạng.
    public static event Action OnRaceFinish;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // Snapshot số người chơi lúc vạch đích xuất hiện. Nếu có người rời
            // giữa chừng thì PlayerLeft() bên dưới sẽ trừ đi.
            TotalPlayers = Runner.ActivePlayers.Count();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        NetworkObject playerObject = other.GetComponent<NetworkObject>();
        if (playerObject == null)
            return;

        // Chỉ xử lý player DO CHÍNH MÁY NÀY sở hữu (mô phỏng vật lý chính xác,
        // không phải bản proxy đồng bộ qua mạng).
        if (!playerObject.HasStateAuthority)
            return;

        RPC_ReportFinish();
    }

    /// <summary>
    /// Lấy người gửi từ chính RPC (info.Source) thay vì tin tham số client gửi,
    /// nên không máy nào khai báo thay người khác được.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportFinish(RpcInfo info = default)
    {
        TryFinishPlayer(info.Source);
    }

    private void TryFinishPlayer(PlayerRef player)
    {
        if (FinishOrder.Contains(player))
            return;

        int rank = FinishOrder.Count + 1;
        FinishOrder.Add(player);

        RPC_NotifyPlayerFinished(player, rank);

        if (FinishOrder.Count == TotalPlayers)
        {
            RPC_NotifyRaceFinished();
        }
        else
        {
            CheckAutoFinishLastPlayer();
        }
    }

    /// <summary>
    /// Chỉ còn đúng 1 người chưa về đích -> tự gán hạng cuối cho họ.
    /// exclude: người vừa rời phòng (phòng trường hợp họ còn nằm trong ActivePlayers).
    /// </summary>
    private void CheckAutoFinishLastPlayer(PlayerRef exclude = default)
    {
        int remaining = TotalPlayers - FinishOrder.Count;
        if (remaining != 1)
            return;

        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            if (p == exclude) continue;

            if (!FinishOrder.Contains(p))
            {
                TryFinishPlayer(p);
                break;
            }
        }
    }

    /// <summary>
    /// Có người rời phòng khi chưa về đích -> không chờ họ nữa, tránh cuộc đua treo.
    /// </summary>
    public void PlayerLeft(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (FinishOrder.Contains(player)) return; // đã có hạng rồi thì không ảnh hưởng

        TotalPlayers--;

        if (FinishOrder.Count == TotalPlayers)
            RPC_NotifyRaceFinished();
        else
            CheckAutoFinishLastPlayer(player);
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

            // Lấy PlayerFinishHandler của nhân vật local qua PlayerContext
            Transform me = PlayerContext.Instance != null ? PlayerContext.Instance.LocalPlayer : null;
            if (me != null)
            {
                var handler = me.GetComponentInChildren<PlayerFinishHandler>();
                if (handler != null) handler.OnFinishedRace(rank);
            }
        }

        RaceSpectatorManager.Instance?.MarkPlayerFinished(player);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyRaceFinished()
    {
        Debug.Log("[FinishLine] Toàn bộ người chơi đã về đích - đua kết thúc.");
        OnRaceFinish?.Invoke();
    }
}