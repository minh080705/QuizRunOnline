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

    [Header("Hồi sinh")]
    [Tooltip("Tên state chạy trong Animator Controller. State Cat_Death không có transition đi ra, nên khi hồi sinh phải ép chuyển về state này.")]
    [SerializeField] private string respawnStateName = "Cat_RunLong";
    [SerializeField] private float respawnCrossFade = 0.1f;

    [Header("Trên bục trao giải")]
    [Tooltip("Tên state ăn mừng (hạng 1). State này không có transition đi ra nên giữ nguyên tới hết cảnh trao giải.")]
    [SerializeField] private string happyStateName = "Cat_IdleHappy";
    [Tooltip("Tên state đứng yên (hạng 2 trở xuống), cũng là state nhân vật đang dùng khi dừng chạy.")]
    [SerializeField] private string idleStateName = "Cat_Idle1";
    [SerializeField] private float podiumCrossFade = 0.25f;

    private bool isDead = false;

    /// <summary>
    /// true trên bản proxy (nhân vật của người khác): Animator do PlayerNetworkState điều khiển theo dữ liệu mạng,
    /// không tự đọc PlayerStats vì moveSpeed của proxy luôn bằng 0.
    /// </summary>
    public bool DrivenRemotely { get; set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Nếu đã chết thì không cập nhật movement nữa
        if (isDead || DrivenRemotely) return;

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

    /// <summary>Hạng 1 trên bục: chạy animation Happy.</summary>
    public void PlayHappy()
    {
        PlayPodiumState(happyStateName);
    }

    /// <summary>Hạng 2 trở xuống trên bục: chạy animation đứng yên (idle).</summary>
    public void PlayIdle()
    {
        PlayPodiumState(idleStateName);
    }

    // Ép chuyển thẳng sang state theo tên (như ResetDeath) thay vì đặt tham số IsRaceFinnished: transition Any State của tham số
    // đó bật "Can Transition To Self", nên giữ bool = true sẽ khiến animation Happy bị khởi động lại liên tục.
    private void PlayPodiumState(string stateName)
    {
        if (isDead) return;

        animator.ResetTrigger(JumpParam);
        animator.ResetTrigger(SlideParam);
        animator.CrossFadeInFixedTime(stateName, podiumCrossFade);
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

        // IsDead/HitWall/Jump/Slide là Trigger: xóa trigger còn tồn đọng, nếu không vừa rời state Death
        // là bị đá ngược vào Death/HitWall ngay.
        animator.ResetTrigger(IsDeadParam);
        animator.ResetTrigger(HitWallParam);
        animator.ResetTrigger(JumpParam);
        animator.ResetTrigger(SlideParam);

        // Cat_Death không có transition đi ra nên phải ép về state chạy. Nếu tên state sai, Unity tự ghi
        // cảnh báo "State could not be found" trong Console.
        animator.CrossFadeInFixedTime(respawnStateName, respawnCrossFade);
    }
}