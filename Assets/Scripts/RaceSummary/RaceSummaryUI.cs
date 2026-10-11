using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng tổng kết trận đấu. Gắn trên object RaceSummary - object này LUÔN BẬT; chỉ Panel (bảng) và nút
/// "Chạm để tiếp tục" được bật/tắt, nhờ vậy script luôn nhận được sự kiện (xem ghi chú ở UI_Notification).
///
/// Luồng: cảnh trao giải xong (HandlePodiumPosArrange.OnArrangeCompleted) -> đợi continueDelay giây ->
/// hiện nút "Chạm để tiếp tục" -> bấm: hiện bảng 4 chỉ số của mọi người chơi (đọc từ LeaderboardManager)
/// -> nút về phòng: rời phòng và về LobbyScene.
/// </summary>
public class RaceSummaryUI : MonoBehaviour
{
    [Header("Bảng tổng kết")]
    [Tooltip("Object Panel (con của RaceSummary), mặc định tắt.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Nơi chứa các hàng người chơi (có Vertical Layout Group).")]
    [SerializeField] private Transform rowContainer;
    [Tooltip("Prefab SummaryRow (asset) có gắn SummaryRowUI.")]
    [SerializeField] private SummaryRowUI rowPrefab;
    [SerializeField] private Button backButton;

    [Tooltip("Tùy chọn: CanvasGroup trên Panel để bảng hiện dần.")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Nút 'Chạm để tiếp tục'")]
    [Tooltip("Nút nằm NGOÀI Panel (con của RaceSummary), mặc định tắt.")]
    [SerializeField] private Button continueButton;
    [Tooltip("Số giây sau khi cảnh trao giải xong thì hiện nút.")]
    [SerializeField] private float continueDelay = 3f;

    [Header("Nguồn sự kiện")]
    [Tooltip("Object HandlePodiumPosArrange trong scene.")]
    [SerializeField] private HandlePodiumPosArrange podium;
    [Tooltip("Chỉ dùng khi chưa gán podium: đợi chừng này giây sau khi trận kết thúc rồi hiện nút.")]
    [SerializeField] private float fallbackDelayAfterRaceEnd = 8f;

    private Coroutine continueRoutine;
    private bool summaryShown;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnEnable()
    {
        if (podium != null)
        {
            podium.OnArrangeCompleted += HandleArrangeCompleted;
        }
        else
        {
            Debug.LogWarning("[RaceSummaryUI] Chưa gán podium: dùng thời gian chờ cố định sau khi trận kết thúc.");
            RaceResultManager.OnRaceFinish += HandleRaceFinishFallback;
        }
    }

    private void OnDisable()
    {
        if (podium != null)
            podium.OnArrangeCompleted -= HandleArrangeCompleted;
        RaceResultManager.OnRaceFinish -= HandleRaceFinishFallback;

        if (continueRoutine != null)
        {
            StopCoroutine(continueRoutine);
            continueRoutine = null;
        }
    }

    // ---------------- Hiện nút "Chạm để tiếp tục" ----------------

    private void HandleArrangeCompleted()
    {
        StartContinueTimer(continueDelay);
    }

    private void HandleRaceFinishFallback()
    {
        StartContinueTimer(fallbackDelayAfterRaceEnd);
    }

    private void StartContinueTimer(float delay)
    {
        if (continueRoutine != null || summaryShown) return;
        continueRoutine = StartCoroutine(ShowContinueButtonAfter(delay));
    }

    private IEnumerator ShowContinueButtonAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        continueRoutine = null;
    }

    private void OnContinueClicked()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        ShowSummary();
    }

    // ---------------- Bảng tổng kết ----------------

    private void ShowSummary()
    {
        if (summaryShown) return;
        summaryShown = true;

        ClearRows(); // bỏ các hàng mẫu đặt sẵn trong scene

        LeaderboardManager leaderboard = LeaderboardManager.Instance;
        if (leaderboard == null)
        {
            Debug.LogWarning("[RaceSummaryUI] Không có LeaderboardManager, bảng sẽ trống.");
        }
        else
        {
            var entries = leaderboard.GetSortedScores(); // đã sắp theo điểm giảm dần
            Fusion.PlayerRef me = leaderboard.Runner != null ? leaderboard.Runner.LocalPlayer : default;

            foreach (LeaderboardEntry entry in entries)
            {
                SummaryRowUI row = Instantiate(rowPrefab, rowContainer);
                row.Setup(entry.Name, entry.Score, entry.Deaths, entry.Coins, entry.Player == me);
            }
        }

        if (panel != null) panel.SetActive(true);

        if (panelGroup != null)
            StartCoroutine(FadeIn());
    }

    private void ClearRows()
    {
        for (int i = rowContainer.childCount - 1; i >= 0; i--)
        {
            GameObject row = rowContainer.GetChild(i).gameObject;
            row.SetActive(false); // Destroy chỉ xóa cuối frame: tắt trước để layout không tính hàng cũ
            Destroy(row);
        }
    }

    private IEnumerator FadeIn()
    {
        panelGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panelGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        panelGroup.alpha = 1f;
    }

    // ---------------- Về phòng ----------------

    private async void OnBackClicked()
    {
        if (backButton != null) backButton.interactable = false;
        await RaceExit.LeaveToLobbyAsync();
    }
}
