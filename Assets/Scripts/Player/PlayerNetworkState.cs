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

    [Networked] public float Speed { get; set; }
    [Networked] public NetworkBool IsJumping { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }

    // Bộ đếm tăng mỗi lần kích hoạt: proxy so với lần trước để bắn trigger đúng 1 lần, kể cả khi rớt gói tin.
    [Networked] public int JumpCount { get; set; }
    [Networked] public int SlideCount { get; set; }

    private int lastJumpCount;
    private int lastSlideCount;
    private bool deathPlayed;

    public override void Spawned()
    {
        if (playerAnimation == null) playerAnimation = GetComponentInChildren<PlayerAnimation>();
        if (stats == null) stats = GetComponentInParent<PlayerStats>();
        if (playerJump == null) playerJump = GetComponentInChildren<PlayerJump>();

        // Proxy: Animator do dữ liệu mạng điều khiển, PlayerAnimation không tự đọc PlayerStats (moveSpeed của proxy luôn 0).
        if (!Object.HasStateAuthority && playerAnimation != null)
            playerAnimation.DrivenRemotely = true;

        // Người vào sau không phát lại các lần nhảy/trượt đã xảy ra.
        lastJumpCount = JumpCount;
        lastSlideCount = SlideCount;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (stats != null) Speed = stats.moveSpeed;
        IsJumping = playerJump != null && playerJump.IsJumping;
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

        playerAnimation.UpdateMovement(Speed, !IsJumping);
    }

    // ---- Gọi từ phía chủ sở hữu ----
    public void NotifyJump() { if (Object.HasStateAuthority) JumpCount++; }
    public void NotifySlide() { if (Object.HasStateAuthority) SlideCount++; }
    public void NotifyDead() { if (Object.HasStateAuthority) IsDead = true; }
    public void NotifyRevived() { if (Object.HasStateAuthority) IsDead = false; }
}
