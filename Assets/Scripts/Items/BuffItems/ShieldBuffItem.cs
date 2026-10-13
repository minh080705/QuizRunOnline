using UnityEngine;

public class ShieldBuffItem : MonoBehaviour
{
    [SerializeField] private ShieldBuff shieldBuff; // giờ tự tham chiếu chính mình, gán được trong Prefab Mode

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
        rewardFlyDemo = FindObjectOfType<RewardFlyDemo>(); // tìm động, không cần kéo-thả
    }

    private void OnTriggerEnter(Collider other)
    {
        // Destroy chỉ có hiệu lực cuối frame: chặn nhặt đúp nếu nhiều collider của player cùng chạm trong frame này.
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;
        if (!stats.Object.HasStateAuthority) return;

        InventoryManager targetInventory = other.GetComponent<InventoryManager>();
        if (targetInventory == null) return;

        targetInventory.AddItem(shieldBuff);
        rewardFlyDemo?.TestReward(); // dùng ?. để tránh lỗi nếu lỡ chưa tìm thấy

        // Item là object cục bộ của máy này; tới đây chắc chắn là player CỦA MÌNH nhặt (người chơi khác đã bị lọc ở
        // HasStateAuthority), nên chỉ ẩn bản trên máy mình, máy người khác vẫn thấy item của họ.
        collected = true;
        SpawnEffect.PlayFollowing(pickupEffect, other.transform, effectOffset);
        Destroy(gameObject);
    }
}