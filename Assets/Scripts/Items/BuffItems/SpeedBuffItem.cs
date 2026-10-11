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
       
        if (!other.CompareTag("Player"))
        {
            
            return;
        }

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null)
        {
            
            return;
        }

       
        if (!stats.Object.HasStateAuthority) return;

        InventoryManager targetInventory = other.GetComponent<InventoryManager>();
        if (targetInventory == null)
        {
            
            return;
        }

       

        targetInventory.AddItem(speedBuff);
        rewardFlyDemo?.TestReward();
    }
}   