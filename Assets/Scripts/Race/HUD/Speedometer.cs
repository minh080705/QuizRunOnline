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
    float lastShownVelocity = -1f; // -1: chưa hiển thị lần nào (tốc độ không bao giờ âm)

    void OnEnable()
    {
        PlayerContext.OnLocalPlayerReady += HandlePlayerReady;

        // Bắt đầu mỗi ván từ 0, chờ player mới đăng ký (không dùng lại stats của ván trước).
        stats = null;
        playerVelocity = 0f;
        lastShownVelocity = -1f;
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

        // Làm tròn 2 chữ số, chỉ đổi chữ khi giá trị hiển thị thật sự đổi. SetText với định dạng không nối chuỗi nên không tạo rác.
        float shown = (float)Math.Round(playerVelocity, 2);
        if (shown == lastShownVelocity) return;
        lastShownVelocity = shown;
        TextMeshPro.SetText("{0:2} km/h", shown);
    }
    public void GetPlayerVelocity()
    {
        playerVelocity = stats.moveSpeed * 2;

    }
}