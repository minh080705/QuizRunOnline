using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinCaculator : MonoBehaviour
{
    public int multiplier = 1 ;
    public static CoinCaculator Instance { get; private set; }
    int tol = 0;

    public static event Action<int> OnCoinChanged;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        
    }
    public void UpdateTotalCoin(int coinValue)
    {
        tol += coinValue * multiplier;
        OnCoinChanged?.Invoke(tol); 
        Debug.Log("Caculator: Total coins: " + GetTotalCoin());
    }
    public int GetTotalCoin()
    {
        return tol;
    }
}
