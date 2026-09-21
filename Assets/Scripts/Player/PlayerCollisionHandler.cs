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
    private void Awake()
    {
        if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimation>();
        stats = GetComponent<PlayerStats>();
    }

    // Dùng nếu Obstacle có Collider KHÔNG phải Trigger
    private void OnCollisionEnter(Collision collision)
    {
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
            
        // 3. Rung màn hình
        if (cameraShake != null)
            cameraShake.Shake(shakeDuration, shakeMagnitude);

        // 4. (Tuỳ chọn) Gọi thêm sự kiện Game Over / mất mạng
        // GameManager.Instance.OnPlayerDie();
    }
}