using UnityEngine;
using TMPro;

public class UI_CountdownText : MonoBehaviour
{
    public static UI_CountdownText Instance;

    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private GameObject countdownPanel; // panel chứa text, để ẩn/hiện cả cụm

    [Header("Tùy chọn hiển thị")]
    [SerializeField] private string goText = "GO!";
    [SerializeField] private float goDisplayDuration = 0.6f;

    private bool _goShown = false;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateCountdown(float remainingSeconds)
    {
        if (countdownPanel != null && !countdownPanel.activeSelf)
            countdownPanel.SetActive(true);

        // Làm tròn lên để hiện 3, 2, 1 thay vì 2.98, 1.87...
        int displaySecond = Mathf.CeilToInt(remainingSeconds);

        if (displaySecond <= 0)
        {
            ShowGo();
        }
        else
        {
            countdownText.text = displaySecond.ToString();
            _goShown = false;
        }
    }

    private void ShowGo()
    {
        if (_goShown) return; // tránh set lại text mỗi frame gây nháy

        countdownText.text = goText;
        _goShown = true;

        // Tự ẩn panel sau khi hiện "GO!" một lúc
        CancelInvoke(nameof(HideCountdown));
        Invoke(nameof(HideCountdown), goDisplayDuration);
    }

    public void HideCountdown()
    {
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }
}