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

        // Bắt đầu mỗi ván từ 0, chờ player mới đăng ký (không dùng lại stats của ván trước).
        stats = null;
        playerVelocity = 0f;
        if (TextMeshPro != null) TextMeshPro.text = "0 km/h";

        // Player có thể đã đăng ký trước khi Speedometer bật (lỡ sự kiện): lấy luôn nếu còn sống.
        if (PlayerContext.Instance != null && PlayerContext.Instance.LocalStats != null)
            stats = PlayerContext.Instance.LocalStats;
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