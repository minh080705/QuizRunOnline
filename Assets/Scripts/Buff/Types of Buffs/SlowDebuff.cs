using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SlowDebuff
{

    [Header("Settings")]
    [SerializeField] public float speedMultiplier = 0.8f;
    [SerializeField] public float buffDuration = 3f;

    [Header("Effect")]
    [SerializeField] public GameObject buffEffectPrefab;    

}
