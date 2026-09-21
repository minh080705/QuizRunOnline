using UnityEngine;

public abstract class BuffItemBase : MonoBehaviour
{
    [Header("Buff Info")]
    public float buffDuration = 5f;

    [Header("Effect")]
    public GameObject buffEffectPrefab;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ApplyBuff(collision.gameObject);

            // chạy hiệu ứng
            BuffImidiately effect =
                collision.GetComponent<BuffImidiately>();

            if (effect != null && buffEffectPrefab != null)
            {
                effect.PlayEffect(buffEffectPrefab, buffDuration);
            }


            // xóa vật phẩm sau khi nhặt
            Destroy(gameObject);
        }
    }


    // Mỗi loại buff tự viết cách hoạt động
    protected abstract void ApplyBuff(GameObject player);
}