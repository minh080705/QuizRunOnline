using UnityEngine;
[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [SerializeField] private PlayerStats playerStats;

    // ---- Tên parameter (dùng hash để tối ưu, tránh gõ sai chuỗi) ----
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
    private static readonly int JumpParam = Animator.StringToHash("Jump");
    private static readonly int SlideParam = Animator.StringToHash("Slide");
    private static readonly int HitWallParam = Animator.StringToHash("HitWall");
    private static readonly int IsDeadParam = Animator.StringToHash("IsDead");

    private bool isDead = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Nếu đã chết thì không cập nhật movement nữa
        if (isDead) return;

        // Tự động đọc từ PlayerLaneSwitcher nếu có gán reference.
        // ĐIỀU CHỈNH: đổi PlayerLaneSwitcher.CurrentSpeed / PlayerLaneSwitcher.IsGrounded
        // cho đúng tên property/field thật sự có trong PlayerLaneSwitcher.cs của bạn.
        if (playerStats != null)
        {
            UpdateMovement(playerStats.moveSpeed, playerStats.isGrounded);
        }
    }
    public void UpdateMovement(float speed, bool isGrounded)
    {
        if (isDead) return;

        animator.SetFloat(SpeedParam, Mathf.Abs(speed));
        animator.SetBool(IsGroundedParam, isGrounded);
    }
        
    /// <summary>Gọi khi player nhảy.</summary>
    public void TriggerJump()
    {
        if (isDead) return;
        animator.ResetTrigger(JumpParam);
        animator.SetTrigger(JumpParam);
    }

    /// <summary>Gọi khi player trượt (slide).</summary>
    public void TriggerSlide()
    {
        if (isDead) return;
        animator.ResetTrigger(SlideParam);
        animator.SetTrigger(SlideParam);
    }

    /// <summary>Gọi từ PlayerCollisionHandler khi va chạm tường.</summary>
    public void TriggerHitWall()
    {
        if (isDead) return;
        animator.ResetTrigger(HitWallParam);
        animator.SetTrigger(HitWallParam);
    }

    /// <summary>Gọi từ PlayerStats khi player chết. Chặn mọi animation khác sau đó.</summary>
    public void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;
        animator.SetBool(IsDeadParam, true);//
    }

    /// <summary>Gọi khi respawn/hồi sinh, reset lại trạng thái animation.</summary>
    public void ResetDeath()
    {
        isDead = false;
        animator.SetBool(IsDeadParam, false);
    }
}