using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Gắn lên GameObject "AddPointsNotification" trong Hierarchy.
/// Tự lắng nghe PlayerScoreManager.OnScoreUpdated (Observer Pattern,
/// giống PlayerScoreUI), tự hiện "+n" rồi trôi lên và mờ dần biến mất.
/// Không toggle SetActive() để tránh xung đột với OnEnable/OnDisable dùng
/// cho việc đăng ký event - thay vào đó ẩn/hiện bằng CanvasGroup.alpha.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class AddPointsNotification : MonoBehaviour
{
    [Header("References (kéo thả TextMeshProUGUI con vào đây)")]
    public TextMeshProUGUI pointsText;

    [Header("Animation Settings")]
    public float duration = 1f;
    public float moveDistance = 60f;
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalAnchoredPosition;
    private Coroutine playingRoutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalAnchoredPosition = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {

        if (PlayerScoreManager.Instance != null)
        {
            PlayerScoreManager.Instance.OnScoreUpdated += HandleScoreUpdated;
        }
        else
        {
        }
    }

    private void OnDisable()
    {
       
        if (PlayerScoreManager.Instance != null)
            PlayerScoreManager.Instance.OnScoreUpdated -= HandleScoreUpdated;
    }

    private void HandleScoreUpdated(int gained, int total, ScoreSource source)
    {
        if (gained <= 0)
        {
            return;
        }

        Show(gained);
    }

    public void Show(int amount)
    {

        if (pointsText != null)
            pointsText.text = $"+{amount}";

        rectTransform.anchoredPosition = originalAnchoredPosition;

        if (playingRoutine != null)
            StopCoroutine(playingRoutine);
        playingRoutine = StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        Vector2 startPos = originalAnchoredPosition;
        Vector2 endPos = startPos + Vector2.up * moveDistance;
        float t = 0f;

        

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            canvasGroup.alpha = alphaCurve.Evaluate(progress);

            yield return null;
        }

       

        canvasGroup.alpha = 0f;
        playingRoutine = null;
    }
}