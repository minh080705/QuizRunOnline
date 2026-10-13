using UnityEngine;

/// <summary>
/// Item nhặt được thì thêm Coin Buff vào túi đồ của người chơi (giống SpeedBuffItem / ShieldBuffItem).
/// Gắn lên prefab CoinBuffItem và gán asset CoinBuff. Item là object cục bộ từng máy: chỉ nhân vật của mình mới nhặt được
/// và chỉ ẩn bản trên máy mình.
/// </summary>
public class CoinBuffItem : MonoBehaviour
{
    [SerializeField] private CoinBuff coinBuff;

    [Header("Hiệu ứng khi nhặt")]
    [Tooltip("Prefab hiệu ứng hạt phát MỘT LẦN và đi theo nhân vật khi người chơi của mình nhặt (tùy chọn). " +
             "Trong prefab hiệu ứng: tắt Looping, Stop Action = None.")]
    [SerializeField] private ParticleSystem pickupEffect;
    [Tooltip("Độ lệch của hiệu ứng so với chân nhân vật (theo trục thế giới).")]
    [SerializeField] private Vector3 effectOffset = new Vector3(0f, 1f, 0f);

    private RewardFlyDemo rewardFlyDemo;
    private bool collected;

    private void Start()
    {
        rewardFlyDemo = FindObjectOfType<RewardFlyDemo>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Destroy chỉ có hiệu lực cuối frame: chặn nhặt đúp nếu nhiều collider của player cùng chạm trong frame này.
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;
        if (!stats.Object.HasStateAuthority) return; // lọc nhân vật của người chơi khác (proxy)

        InventoryManager targetInventory = other.GetComponent<InventoryManager>();
        if (targetInventory == null) return;

        targetInventory.AddItem(coinBuff);
        rewardFlyDemo?.TestReward();

        collected = true;
        SpawnEffect.PlayFollowing(pickupEffect, other.transform, effectOffset);
        Destroy(gameObject);
    }
}
