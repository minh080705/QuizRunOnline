using UnityEngine;

public class SpeedBuffItem : MonoBehaviour
{
    [SerializeField] private SpeedBuff speedBuff;

    private RewardFlyDemo rewardFlyDemo;

    private void Start()
    {
        rewardFlyDemo = FindObjectOfType<RewardFlyDemo>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SpeedBuffItem:{gameObject.name}] OnTriggerEnter chạm bởi {other.name}, tag={other.tag}");

        if (!other.CompareTag("Player"))
        {
            Debug.Log($"[SpeedBuffItem:{gameObject.name}] Bỏ qua vì không phải tag Player");
            return;
        }

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null)
        {
            Debug.Log($"[SpeedBuffItem:{gameObject.name}] Bỏ qua vì không tìm thấy PlayerStats");
            return;
        }

        Debug.Log($"[SpeedBuffItem:{gameObject.name}] HasStateAuthority={stats.Object.HasStateAuthority}");
        if (!stats.Object.HasStateAuthority) return;

        InventoryManager targetInventory = other.GetComponent<InventoryManager>();
        if (targetInventory == null)
        {
            Debug.Log($"[SpeedBuffItem:{gameObject.name}] Bỏ qua vì không tìm thấy InventoryManager");
            return;
        }

        Debug.Log($"[SpeedBuffItem:{gameObject.name}] speedBuff null? {speedBuff == null}");

        targetInventory.AddItem(speedBuff);
        rewardFlyDemo?.TestReward();
    }
}   