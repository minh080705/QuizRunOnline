// chạy các hiệu ứng buff trên player, ví dụ như hiệu ứng tăng tốc, hiệu ứng khiên, v.v.
using UnityEngine;
using System.Collections;


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