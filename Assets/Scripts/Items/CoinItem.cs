using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinItem : MonoBehaviour
{
    [SerializeField] int coinValueSet = 1;
    public int coinValue => coinValueSet;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CoinCaculator.Instance.UpdateTotalCoin(coinValue);

            Destroy(gameObject);
            Debug.Log("Coin collected! Total coins: " + CoinCaculator.Instance.GetTotalCoin());
        }
    }

    
        
}
