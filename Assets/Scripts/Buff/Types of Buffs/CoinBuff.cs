//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//[System.Serializable]
//public class CoinBuff : Buff
//{

//    [Header("Settings")]
//    [SerializeField] private int coinMultiplier = 2;
//    [SerializeField] private PlayerStats player;
//    private int coinMultiplierBefore = 1;

//    private GameObject currentEffect;
//    public override void ApplyBuff()
//    {
//        coinMultiplierBefore = CoinCaculator.Instance.multiplier;
//        CoinCaculator.Instance.multiplier = coinMultiplier;
//        StartCoroutine(CoinBuffTimeout());
//    }

//    public void PlayEffect(GameObject effectPrefab, float duration)
//    {
//        if (currentEffect != null)
//        {
//            Destroy(currentEffect);
//        }

//        if (effectPrefab != null)
//        {
//            currentEffect = Instantiate(
//                effectPrefab,
//                player.transform.position,
//                Quaternion.identity,
//                player.transform
//            );
//        }

//        StartCoroutine(RemoveEffect(duration));
//    }

//    private IEnumerator RemoveEffect(float time)
//    {
//        yield return new WaitForSeconds(time);
//        if (currentEffect != null)
//        {
//            Destroy(currentEffect);
//        }
//    }
//    private IEnumerator CoinBuffTimeout()
//    {
//        yield return new WaitForSeconds(buffDuration);   
//        CoinCaculator.Instance.multiplier = coinMultiplierBefore;           
//    }

//}
