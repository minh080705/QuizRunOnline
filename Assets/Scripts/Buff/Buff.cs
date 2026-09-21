using UnityEngine;

public abstract class Buff : ScriptableObject
{
    [Header("Thông tin Buff")]
    public float buffDuration = 5f;
    public Sprite icon;
    public int maxStack = 3;

    [Header("Effect")]
    public GameObject buffEffectPrefab;

    // Gọi đúng 1 lần lúc buff bắt đầu áp dụng (từ 0 lên 1 stack)
    public abstract void OnFirstApplied(PlayerStats player, BuffContext context);

    // Gọi mỗi khi số stack thay đổi (tăng lúc nhặt thêm, giảm lúc hết hạn)
    public abstract void OnStackChanged(PlayerStats player, int currentStack, BuffContext context);

    // Hàm tiện ích — item/UI vẫn gọi y hệt như code cũ, không cần đổi gì ở nơi gọi
    public void ApplyBuff(PlayerStats targetPlayer)
    {
        if (!targetPlayer.Object.HasStateAuthority) return;

        var manager = targetPlayer.GetComponent<PlayerBuffManager>();
        if (manager != null) manager.ApplyStack(this);
    }
}


//using UnityEngine;

//public abstract class Buff : MonoBehaviour
//{
//    [Header("Thông tin Buff")]
//    public float buffDuration = 5f;
//    public Sprite icon;

//    [Header("Effect")]
//    [SerializeField] public GameObject buffEffectPrefab;

//    public abstract void ApplyBuff(PlayerStats targetPlayer); 
//}