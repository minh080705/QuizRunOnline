using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UI_ConfirmDialog : MonoBehaviour
{
    public static UI_ConfirmDialog Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirmCallback;
    private Action onCancelCallback;

    void Awake()
    {
        if (Instance == null) Instance = this;
        dialogPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string message, Action onConfirm, Action onCancel = null)
    {
        messageText.text = message;
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        // Gỡ listener cũ trước khi thêm mới, tránh gọi trùng nếu Show() được gọi nhiều lần
        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        confirmButton.onClick.AddListener(HandleConfirm);
        cancelButton.onClick.AddListener(HandleCancel);

        dialogPanel.SetActive(true);
    }

    private void HandleConfirm()
    {
        dialogPanel.SetActive(false);
        onConfirmCallback?.Invoke();
    }

    private void HandleCancel()
    {
        dialogPanel.SetActive(false);
        onCancelCallback?.Invoke();
    }
}