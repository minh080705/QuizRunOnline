using Fusion;
using UnityEngine;

/// <summary>
/// Đồng bộ trạng thái animation của nhân vật qua mạng (Shared mode).
/// Chủ sở hữu (StateAuthority) ghi trạng thái; các máy khác (proxy) đọc và điều khiển Animator của bản proxy.
/// Thêm vào cùng prefab với PlayerStats (cùng NetworkObject).
/// </summary>
public class PlayerNetworkState : NetworkBehaviour
{
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerJump playerJump;

    [Header("Bù độ trễ proxy (chỉ hiển thị, xem ProxyLagCompensator)")]
    [SerializeField] private bool proxyLeadEnabled = true;
    [Tooltip("Hệ số nhân với (RTT_mình + RTT_họ)/2. GetPlayerRtt gồm cả thời gian xử lý nên < 1.")]
    [SerializeField, Range(0f, 1.5f)] private float latencyScale = 0.75f;
    [Tooltip("Bù thêm cho bộ đệm nội suy của Fusion (giây).")]
    [SerializeField] private float baseLead = 0.05f;
    [Tooltip("Trần thời gian bù (giây), chặn proxy chạy lố khi người đó dừng đột ngột.")]
    [SerializeField] private float maxLead = 0.25f;
    [SerializeField] private float leadSmoothTime = 0.15f;
    [Tooltip("In log ping / lead / offset của từng proxy mỗi giây, để hiệu chỉnh. Tắt khi xong.")]
    [SerializeField] private bool debugProxyLead = false;

    private const float PingPublishInterval = 0.5f;
    private float pingPublishTimer;

    [Networked] public float Speed { get; set; }
    /// <summary>RTT (ms, làm tròn 10 ms) do chủ nhân vật tự đăng. Máy khác không đọc được ping của người khác, nên phải đi đường này.</summary>
    [Networked] public int PingMs { get; set; }
    [Networked] public NetworkBool IsJumping { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    /// <summary>Đang ăn mừng trên bục (hạng 1): máy khác chạy animation Happy cho bản proxy.</summary>
    [Networked] public NetworkBool IsCelebrating { get; set; }

    // Bộ đếm tăng mỗi lần kích hoạt: proxy so với lần trước để bắn trigger đúng 1 lần, kể cả khi rớt gói tin.
    [Networked] public int JumpCount { get; set; }
    [Networked] public int SlideCount { get; set; }

    private int lastJumpCount;
    private int lastSlideCount;
    private bool deathPlayed;
    private bool celebrationPlayed;

    public override void Spawned()
    {
        if (playerAnimation == null) playerAnimation = GetComponentInChildren<PlayerAnimation>();
        if (stats == null) stats = GetComponentInParent<PlayerStats>();
        if (playerJump == null) playerJump = GetComponentInChildren<PlayerJump>();

        // Proxy: Animator do dữ liệu mạng điều khiển, PlayerAnimation không tự đọc PlayerStats (moveSpeed của proxy luôn 0).
        if (!Object.HasStateAuthority && playerAnimation != null)
            playerAnimation.DrivenRemotely = true;

        // Proxy: bù độ trễ hiển thị theo ping. Gắn runtime nên không phải sửa prefab.
        if (!Object.HasStateAuthority && proxyLeadEnabled && !TryGetComponent<ProxyLagCompensator>(out _))
        {
            var compensator = gameObject.AddComponent<ProxyLagCompensator>();
            compensator.Init(this, latencyScale, baseLead, maxLead, leadSmoothTime, debugProxyLead);
        }

        // Người vào sau không phát lại các lần nhảy/trượt đã xảy ra.
        lastJumpCount = JumpCount;
        lastSlideCount = SlideCount;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (stats != null) Speed = stats.moveSpeed;
        IsJumping = playerJump != null && playerJump.IsJumping;

        // Đăng RTT của máy mình ~2 lần/giây, làm tròn 10 ms để giá trị ít đổi (biến mạng chỉ gửi khi thay đổi).
        pingPublishTimer -= Runner.DeltaTime;
        if (pingPublishTimer <= 0f)
        {
            pingPublishTimer = PingPublishInterval;
            PingMs = Mathf.RoundToInt((float)Runner.GetPlayerRtt(Runner.LocalPlayer) * 100f) * 10;
        }
    }

    public override void Render()
    {
        if (Object.HasStateAuthority || playerAnimation == null) return;

        if (JumpCount != lastJumpCount)
        {
            lastJumpCount = JumpCount;
            playerAnimation.TriggerJump();
        }

        if (SlideCount != lastSlideCount)
        {
            lastSlideCount = SlideCount;
            playerAnimation.TriggerSlide();
        }

        if (IsDead && !deathPlayed)
        {
            deathPlayed = true;
            playerAnimation.TriggerDeath();
        }
        else if (!IsDead && deathPlayed)
        {
            // Chủ sở hữu vừa hồi sinh: đưa Animator của bản proxy về chạy lại.
            deathPlayed = false;
            playerAnimation.ResetDeath();
        }

        // Hạng 1 lên bục: chạy Happy đúng một lần (người vào sau khi đã ăn mừng cũng thấy vì cờ nằm trong trạng thái mạng).
        if (IsCelebrating && !celebrationPlayed)
        {
            celebrationPlayed = true;
            playerAnimation.PlayHappy();
        }
        else if (!IsCelebrating)
        {
            celebrationPlayed = false;
        }

        playerAnimation.UpdateMovement(Speed, !IsJumping);
    }

    public void NotifyJump() { if (Object.HasStateAuthority) JumpCount++; }
    public void NotifySlide() { if (Object.HasStateAuthority) SlideCount++; }
    public void NotifyDead() { if (Object.HasStateAuthority) IsDead = true; }
    public void NotifyRevived() { if (Object.HasStateAuthority) IsDead = false; }
    public void NotifyCelebrate(bool celebrating) { if (Object.HasStateAuthority) IsCelebrating = celebrating; }
}
