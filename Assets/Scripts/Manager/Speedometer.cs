using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Speedometer : MonoBehaviour
{
    private PlayerStats stats;
    [SerializeField] private TextMeshProUGUI TextMeshPro;
    float playerVelocity = 0f;

    void OnEnable()
    {
        PlayerContext.OnLocalPlayerReady += HandlePlayerReady;
    }

    void OnDisable()
    {
        PlayerContext.OnLocalPlayerReady -= HandlePlayerReady;
    }

    void HandlePlayerReady(Transform localPlayer)
    {
        stats = PlayerContext.Instance.LocalStats;
    }

    void Start()
    {

    }


    void LateUpdate()
    {
        if (stats == null) return; // chưa có player thì chưa làm gì

        GetPlayerVelocity();

        TextMeshPro.text = Math.Round(playerVelocity, 2) + " km/h";
    }
    public void GetPlayerVelocity()
    {
        playerVelocity = stats.moveSpeed * 2;

    }
}