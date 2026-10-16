using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;

/// <summary>
/// Thông báo dạng hàng đợi (mạng và cục bộ), hiển thị tuần tự. Panel luôn active và chỉ ẩn/hiện bằng CanvasGroup.alpha
/// để các component con dùng chung parent vẫn nhận được Start/OnEnable.
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

        canvasGroup.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Dùng cho: nhận vật phẩm tăng tốc, nhận khiên, nhận x2 vàng, đã sử dụng vật phẩm
    public void ShowLocalMessage(string message)
    {
        Enqueue(message);
    }

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