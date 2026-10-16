using System;
using System.Collections;
using Fusion;
using UnityEngine;

/// <summary>
/// Hệ thống 3 mạng. Gắn trên prefab nhân vật (cùng NetworkObject với PlayerStats).
///
/// Mỗi lần va chạm vật cản (PlayerCollisionHandler gọi OnPlayerDied sau khi đã cho nhân vật dừng và chạy
/// animation chết) trừ 1 mạng:
/// - Còn mạng: sau respawnDelay giây hồi sinh tại chỗ, chạy lại với tốc độ cũ, kèm buff khiên.
/// - Hết mạng: bị loại, không hồi sinh nữa; báo cho RaceResultManager (xếp hạng theo khoảng cách tới đích)
///   và chuyển camera sang chế độ quan sát.
/// Logic chỉ chạy trên máy sở hữu nhân vật; máy khác nhận trạng thái chết/hồi sinh qua PlayerNetworkState.
/// </summary>
public class PlayerLives : NetworkBehaviour
{
    /// Nhân vật do CHÍNH máy này điều khiển (HUD đọc số mạng ở đây).
    public static PlayerLives Local { get; private set; }

    [Header("Mạng")]
    [SerializeField] private int maxLives = 3;

    [Header("Hồi sinh")]
    [Tooltip("Số giây chờ trước khi hồi sinh.")]
    [SerializeField] private float respawnDelay = 5f;

    [Tooltip("Buff khiên áp lên nhân vật lúc hồi sinh (kéo asset ShieldBuff vào). Thời gian khiên = buffDuration của asset.")]
    [SerializeField] private Buff respawnShield;

    [Tooltip("Hệ số nhân tốc độ trước lúc chết khi chạy lại. 1 = giữ nguyên tốc độ cũ.")]
    [SerializeField] private float respawnSpeedFactor = 1f;

    [Header("Bị loại (hết mạng)")]
    [Tooltip("Chờ chừng này giây (để thấy animation chết) rồi mới chuyển camera sang quan sát.")]
    [SerializeField] private float spectateDelay = 2f;

    [Networked] public int Lives { get; private set; }

    public int MaxLives => maxLives;

    /// Số lần đã chết = số mạng đã mất (3 nếu đã hết mạng).
    public int Deaths => Mathf.Max(0, maxLives - Lives);

    /// Bắn trên máy chủ sở hữu mỗi khi số mạng của nhân vật LOCAL thay đổi (lúc spawn và mỗi lần chết).
    /// PlayerScoreNetworkReporter dùng để báo số lần chết lên bảng chung.
    public static event Action OnLocalLivesChanged;

    /// Số giây còn lại trước khi hồi sinh (0 = không đang chờ). Chỉ có giá trị trên máy chủ sở hữu.
    public float RespawnRemaining { get; private set; }

    /// Đã hết mạng và bị loại (đọc từ PlayerStats để mọi máy cùng thấy).
    public bool IsEliminated => stats != null && stats.IsEliminated;

    private PlayerStats stats;
    private PlayerAnimation playerAnimation;
    private PlayerCollisionHandler collisionHandler;
    private PlayerNetworkState netState;
    private PlayerFinishHandler finishHandler;

    private void Awake()
    {
        stats = GetComponentInParent<PlayerStats>();
        playerAnimation = GetComponentInChildren<PlayerAnimation>();
        collisionHandler = GetComponentInChildren<PlayerCollisionHandler>();
        netState = GetComponentInParent<PlayerNetworkState>();
        finishHandler = GetComponentInChildren<PlayerFinishHandler>();
    }

    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;

        Lives = maxLives;
        Local = this;
        OnLocalLivesChanged?.Invoke();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this) Local = null;
    }

    /// <summary>
    /// Gọi từ PlayerCollisionHandler ngay sau khi nhân vật va chạm và đã dừng + chạy animation chết.
    /// </summary>
    public void OnPlayerDied()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority) return;
        if (IsEliminated) return;

        Lives = Mathf.Max(0, Lives - 1);
        OnLocalLivesChanged?.Invoke();

        if (Lives > 0)
        {
            UI_Notification.Instance?.ShowLocalMessage($"Bạn còn {Lives} mạng!");
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            UI_Notification.Instance?.ShowLocalMessage("Bạn đã hết mạng!");
            StartCoroutine(EliminationRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        RespawnRemaining = respawnDelay;

        while (RespawnRemaining > 0f)
        {
            yield return null;
            RespawnRemaining -= Time.deltaTime;

            // Trận đã kết thúc / nhân vật đã về đích trong lúc chờ -> không hồi sinh nữa.
            if (!CanRespawn())
            {
                RespawnRemaining = 0f;
                yield break;
            }
        }

        RespawnRemaining = 0f;
        Respawn();
    }

    private bool CanRespawn()
    {
        if (IsEliminated) return false;
        if (stats == null || stats.hasFinishedRace) return false;
        if (finishHandler != null && finishHandler.HasFinished) return false;
        if (RaceResultManager.Instance != null && RaceResultManager.Instance.IsRaceFinished) return false;
        return true;
    }

    private void Respawn()
    {
        // Chạy lại với tốc độ trước lúc chết
        stats.Revive(respawnSpeedFactor);

        // Nhận lại va chạm, trả animation về trạng thái chạy, báo cho các máy khác
        if (collisionHandler != null) collisionHandler.ResetForRespawn();
        if (playerAnimation != null) playerAnimation.ResetDeath();
        if (netState != null) netState.NotifyRevived();

        // Khiên bảo vệ lúc mới hồi sinh (vật cản đã gây chết vẫn đang ở ngay đó)
        if (respawnShield != null)
            respawnShield.ApplyBuff(stats);
        else
            Debug.LogWarning("[PlayerLives] Chưa gán Respawn Shield: nhân vật sẽ hồi sinh mà không có khiên.");
    }

    private IEnumerator EliminationRoutine()
    {
        // Ghi nhận ngay để các máy khác (và bộ sinh vật cản của Master) bỏ qua người này.
        stats.IsEliminated = true;

        // Báo trọng tài kèm Z lúc bị loại: hạng của người bị loại được chốt cuối trận theo khoảng cách tới đích.
        if (RaceResultManager.Instance != null)
            RaceResultManager.Instance.ReportEliminated(transform.position.z);
        else
            Debug.LogError("[PlayerLives] Không có RaceResultManager trong scene, không xếp hạng được người bị loại.");

        yield return new WaitForSeconds(spectateDelay);

        // Nếu cả trận đã kết thúc thì camera sắp chuyển sang cảnh trao giải, không chuyển sang quan sát nữa.
        bool raceOver = RaceResultManager.Instance != null && RaceResultManager.Instance.IsRaceFinished;
        if (!raceOver)
            RaceSpectatorManager.Instance?.ActivateSpectatorMode();
    }
}
