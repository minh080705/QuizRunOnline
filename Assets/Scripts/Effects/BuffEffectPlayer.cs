using UnityEngine;
using System.Collections;

/// <summary>Chạy hiệu ứng buff trên nhân vật (tăng tốc, khiên...).</summary>
public class BuffEffectPlayer : MonoBehaviour
{
    private GameObject currentEffect;

    public void PlayEffect(
        GameObject effectPrefab,
        float duration
    )
    {
        // xóa effect cũ
        if (currentEffect != null)
        {
            Destroy(currentEffect);
        }

        // tạo effect mới
        currentEffect = Instantiate(
            effectPrefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        StartCoroutine(
            RemoveEffect(duration)
        );  
    }

    IEnumerator RemoveEffect(float time)
    {
        yield return new WaitForSeconds(time);

        if (currentEffect != null)
        {
            Destroy(currentEffect);
        }
    }
}