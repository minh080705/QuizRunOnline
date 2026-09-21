using System;
using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private PlayerInput inputHandler;
    [SerializeField] private PlayerCollisionController collisionController;
    [SerializeField] private PlayerSlide playerSlide;


    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float jumpDuration = 0.6f;
    [SerializeField]
    private AnimationCurve jumpCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    [SerializeField] private float jumpBufferTime = 0.2f;
    public event Action OnJumpEnded;
    public bool IsJumping { get; private set; }
    private float jumpTimer;
    private float groundY;
    private float lastJumpPressTime = -999f;
    private void Start()
    {
        groundY = transform.position.y;
    }

    private void OnEnable()
    {
        if (inputHandler != null)
            inputHandler.OnJumpPressed += HandleJumpPressed;

        if (playerSlide != null)
            playerSlide.OnSlideEnded += HandleSlideEnded;
    }

    private void OnDisable()
    {
        if (inputHandler != null)
            inputHandler.OnJumpPressed -= HandleJumpPressed;

        if (playerSlide != null)
            playerSlide.OnSlideEnded -= HandleSlideEnded;
    }

    private void HandleJumpPressed()
    {
        lastJumpPressTime = Time.time;
        if (playerSlide != null && playerSlide.IsSliding) return; // chặn nhảy khi đang trượt
        StartJump();
        
    }
    private void HandleSlideEnded()
    {
        bool hasRecentJumpInput = Time.time - lastJumpPressTime <= jumpBufferTime;
        if (hasRecentJumpInput && !IsJumping)
        {
            StartJump(); // thực hiện thao tác nhảy "trễ" mà người chơi đã bấm ngay trước khi slide xong
        }
    }

    private void Update()
    {
        UpdateJump();
    }

    public void StartJump()
    {
        if (IsJumping) return;
        IsJumping = true;
        jumpTimer = 0f;
        collisionController.SetJump();
        playerAnimation.TriggerJump();
    }

    private void UpdateJump()
    {
        if (!IsJumping) return;

        jumpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTimer / jumpDuration);
        float heightOffset = jumpCurve.Evaluate(t) * jumpHeight;

        Vector3 pos = transform.position;
        pos.y = groundY + heightOffset;
        transform.position = pos;

        if (t >= 1f)
            EndJump();
    }

    private void EndJump()
    {
        IsJumping = false;
        jumpTimer = 0f;

        Vector3 pos = transform.position;
        pos.y = groundY;
        transform.position = pos;

        collisionController.SetNormal();
        OnJumpEnded?.Invoke();
    }

    

   

    
}