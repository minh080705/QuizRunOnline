using Fusion;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerCollisionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private CameraShake cameraShake; // script rung camera bên dưới

    [Header("Settings")]
    [SerializeField] private string obstacleTag = "Obstacle";
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    private bool isDead = false; // tránh trigger nhiều lần
    private PlayerStats stats; 
    public bool isShieldOn = false; //trạng thái khi nhân vật đang có Shield Buff
    private NetworkObject netObject;
    private PlayerNetworkState netState;
    private PlayerLives lives; // có thì mỗi lần va chạm trừ 1 mạng và hồi sinh; không có thì chết hẳn như cũ

    // Chỉ nhân vật do máy này sở hữu mới tự xử lý va chạm; proxy chết khi nhận IsDead từ mạng.
    private bool IsAuthority => netObject == null || !netObject.IsValid || netObject.HasStateAuthority;

    private void Awake()
    {
        if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimation>();
        stats = GetComponent<PlayerStats>();
        netObject = GetComponentInParent<NetworkObject>();
        netState = GetComponentInParent<PlayerNetworkState>();
        lives = GetComponentInParent<PlayerLives>();
    }

    /// <summary>Gọi khi hồi sinh để nhận lại va chạm với vật cản (khiên buff lo phần bảo vệ lúc đầu).</summary>
    public void ResetForRespawn()
    {
        isDead = false;
    }

    // Dùng nếu Obstacle có Collider KHÔNG phải Trigger
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsAuthority) return;
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            if (!isShieldOn)
            {
                HandleObstacleHit();
            }
        }
    }

    // Dùng nếu Obstacle có Collider LÀ Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (!IsAuthority) return;
        if (other.CompareTag(obstacleTag))
        {
            if (!isShieldOn)
            {
                HandleObstacleHit();
            }   
        }
    }

    private void HandleObstacleHit()
    {
        if (isDead) return;
        isDead = true;

        // 1. Dừng player chạy
        if (stats != null) {
            stats.Dead();
        }


        // 2. Chuyển animation sang Hitwall / Die
        playerAnimation.TriggerDeath();
        if (netState != null) netState.NotifyDead();

        // 3. Rung màn hình (prefab không giữ được tham chiếu camera trong scene nên tìm lúc runtime)
        if (cameraShake == null)
            cameraShake = FindObjectOfType<CameraShake>();
        if (cameraShake != null)
            cameraShake.Shake(shakeDuration, shakeMagnitude);

        // 4. Trừ 1 mạng: còn mạng thì hồi sinh sau vài giây, hết mạng thì bị loại và chuyển sang quan sát
        if (lives != null)
            lives.OnPlayerDied();
    }
}