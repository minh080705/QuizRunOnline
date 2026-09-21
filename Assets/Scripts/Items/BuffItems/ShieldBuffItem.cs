using UnityEngine;

public class ShieldBuffItem : MonoBehaviour
{
    [SerializeField] private ShieldBuff shieldBuff; // giờ tự tham chiếu chính mình, gán được trong Prefab Mode

    private RewardFlyDemo rewardFlyDemo;

    private void Start()
    {
        rewardFlyDemo = FindObjectOfType<RewardFlyDemo>(); // tìm động, không cần kéo-thả
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;
        if (!stats.Object.HasStateAuthority) return;

        InventoryManager targetInventory = other.GetComponent<InventoryManager>();
        if (targetInventory == null) return;

        targetInventory.AddItem(shieldBuff);
        rewardFlyDemo?.TestReward(); // dùng ?. để tránh lỗi nếu lỡ chưa tìm thấy
    }
}