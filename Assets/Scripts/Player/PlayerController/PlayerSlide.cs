using System;
using Fusion;
using UnityEngine;

public class PlayerSlide : MonoBehaviour
{
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private PlayerInput inputHandler;
    [SerializeField] private PlayerCollisionController collisionController;
    [SerializeField] private float slideDuration = 1f;
    [SerializeField] private PlayerJump playerJump;
    [SerializeField] private float slideBufferTime = 0.2f;
    public bool IsSliding { get; private set; }
    private float slideTimer;
    private float lastSlidePressTime = -999f;
    public event Action OnSlideEnded;

    private NetworkObject netObject;
    private PlayerNetworkState netState;
    private PlayerStats playerStats;

    // Chỉ nhân vật do máy này sở hữu mới nhận input và tự trượt; proxy lấy animation từ mạng.
    private bool IsAuthority => netObject == null || !netObject.IsValid || netObject.HasStateAuthority;

    private void Awake()
    {
        netObject = GetComponentInParent<NetworkObject>();
        netState = GetComponentInParent<PlayerNetworkState>();
        playerStats = GetComponentInParent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (inputHandler != null)
            inputHandler.OnSlidePressed += HandleSlidePressed;
        if (playerJump != null)
            playerJump.OnJumpEnded += HandleJumpEnded;
    }

    private void OnDisable()
    {
        if (inputHandler != null)
            inputHandler.OnSlidePressed -= HandleSlidePressed;
        if (playerJump != null)
            playerJump.OnJumpEnded -= HandleJumpEnded;
    }

    private void Update()
    {
        if (!IsAuthority) return;
        UpdateSlide();
    }
    private void HandleSlidePressed()
    {
        if (!IsAuthority) return;
        lastSlidePressTime = Time.time;
        if (playerJump != null && playerJump.IsJumping) return; // chặn trượt khi đang nhảy
        StartSlide();
            
    }
    private void HandleJumpEnded()
    {
        if (!IsAuthority) return;
        bool hasRecentSlideInput = Time.time - lastSlidePressTime <= slideBufferTime;
        if (hasRecentSlideInput && !IsSliding)
        {
            StartSlide(); // thực hiện thao tác nhảy "trễ" mà người chơi đã bấm ngay trước khi slide xong
        }
    }
    public void StartSlide()
    {
        if (IsSliding) return;
        if (playerStats != null && playerStats.isFrozenByQuestion) return; // đang dừng để trả lời câu hỏi
        IsSliding = true;
        slideTimer = 0f;
        collisionController.SetSlide();
        playerAnimation.TriggerSlide();
        if (netState != null) netState.NotifySlide();
    }

    private void UpdateSlide()
    {
        if (!IsSliding) return;

        slideTimer += Time.deltaTime;
        if (slideTimer >= slideDuration)
        {
            EndSlide();
        }
    }

    private void EndSlide()
    {
        IsSliding = false;
        slideTimer = 0f;
        collisionController.SetNormal();
        OnSlideEnded?.Invoke();
    }
}