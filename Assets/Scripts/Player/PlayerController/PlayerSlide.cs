using System;
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
        UpdateSlide();
    }
    private void HandleSlidePressed()
    {
        lastSlidePressTime = Time.time;
        if (playerJump != null && playerJump.IsJumping) return; // chặn trượt khi đang nhảy
        StartSlide();
            
    }
    private void HandleJumpEnded()
    {
        bool hasRecentSlideInput = Time.time - lastSlidePressTime <= slideBufferTime;
        if (hasRecentSlideInput && !IsSliding)
        {
            StartSlide(); // thực hiện thao tác nhảy "trễ" mà người chơi đã bấm ngay trước khi slide xong
        }
    }
    public void StartSlide()
    {
        if (IsSliding) return;
        IsSliding = true;
        slideTimer = 0f;
        collisionController.SetSlide();
        playerAnimation.TriggerSlide();
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