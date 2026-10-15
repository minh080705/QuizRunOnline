using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;

/// <summary>
/// Quản lý thông báo dạng hàng đợi (network + local), hiển thị tuần tự.
///
/// SỬA QUAN TRỌNG: bỏ hoàn toàn notificationPanel.SetActive(true/false).
/// Trước đây Awake() gọi SetActive(false) lên panel - nếu panel này VÔ TÌNH
/// là cha (hoặc chính) của các component khác dùng pattern "subscribe event
/// trong Start()/OnEnable()" (ví dụ AddPointsNotification), việc tắt panel
/// sẽ chặn luôn Start()/OnEnable() của các component con đó, khiến chúng
/// không bao giờ nhận được event dù logic hoàn toàn đúng.
///
/// Panel giờ LUÔN active, chỉ ẩn/hiện qua CanvasGroup.alpha (coroutine Fade
/// sẵn có đã tự lo việc này) - an toàn để dùng chung parent với các UI khác.
/// </summary>
public class UI_Notification : NetworkBehaviour
{
    public static UI_Notification Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeDuration = 0.3f;

    private Queue<string> messageQueue = new Queue<string>();
    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // Không SetActive(false) panel nữa - chỉ ẩn qua alpha.
        // Panel luôn active, an toàn cho các component con khác dùng chung.
        canvasGroup.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ==================== LOCAL (chỉ máy này thấy) ====================
    // Dùng cho: nhận vật phẩm tăng tốc, nhận khiên, nhận x2 vàng, đã sử dụng vật phẩm
    public void ShowLocalMessage(string message)
    {
        Enqueue(message);
    }

    // ==================== NETWORKED (mọi máy đều thấy) ====================
    // Dùng cho: player rời đường đua, player về đích, player bất tỉnh
    public void ShowNetworkMessage(string message)
    {
        RPC_ShowNetworkMessage(message);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ShowNetworkMessage(string message)
    {
        Enqueue(message);
    }

    // ==================== Hiển thị tuần tự ====================
    private void Enqueue(string message)
    {
        messageQueue.Enqueue(message);
        if (!isShowing)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isShowing = true;

        while (messageQueue.Count > 0)
        {
            string message = messageQueue.Dequeue();
            notificationText.text = message;

            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
            yield return new WaitForSeconds(displayDuration);
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
        }

        isShowing = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}