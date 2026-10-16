using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng icon phần thưởng xuất hiện tại một vị trí, bay về icon đích rồi biến mất (Coroutine + AnimationCurve).
/// </summary>
public class RewardFlyDemo : MonoBehaviour
{
    [Header("Tham chiếu bắt buộc")]
    [SerializeField] private RectTransform rewardIconPrefab; // icon gốc (đang tắt active)
    [SerializeField] private RectTransform targetIcon;        // vị trí đích (vd icon túi đồ)
    [SerializeField] private Canvas canvas;

    [Header("Tùy chỉnh hiệu ứng")]
    [SerializeField] private float popDuration = 0.25f;   // thời gian pop-in xuất hiện
    [SerializeField] private float holdDuration = 0.2f;   // đứng yên 1 chút trước khi bay
    [SerializeField] private float flyDuration = 0.6f;    // thời gian bay về đích
    [SerializeField] private float arcHeight = 120f;      // độ cao vòng cung khi bay (0 = bay thẳng)
    [SerializeField]
    private AnimationCurve flyEase =
        AnimationCurve.EaseInOut(0, 0, 1, 1);             // easing cho đường bay

    public void TestReward()
    {
        Vector3 centerScreen = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        PlayRewardEffect(centerScreen, () =>
        {
            StartCoroutine(PunchScale(targetIcon, 0.25f, 0.25f));
        });
    }

    /// <summary>
    /// Hàm chính: gọi khi hoàn thành nhiệm vụ trong game thật.
    /// startScreenPos: vị trí xuất hiện (toạ độ screen space, vd vị trí nhân vật/quái).
    /// onArrived: callback khi item bay tới đích (dùng để cộng số lượng thật vào inventory).
    /// </summary>
    public void PlayRewardEffect(Vector3 startScreenPos, Action onArrived = null)
    {
        RectTransform icon = Instantiate(rewardIconPrefab, canvas.transform);
        icon.gameObject.SetActive(true);
/// <summary>Chạy thử hiệu ứng ở giữa màn hình (gắn vào OnClick của Button).</summary>
        icon.position = startScreenPos;
        icon.localScale = Vector3.zero;

        StartCoroutine(RewardRoutine(icon, onArrived));
    }

    private IEnumerator RewardRoutine(RectTransform icon, Action onArrived)
    {
        yield return ScaleTo(icon, 0f, 1.2f, popDuration * 0.7f);
        yield return ScaleTo(icon, 1.2f, 1f, popDuration * 0.3f);

        yield return new WaitForSeconds(holdDuration);

        Vector3 startPos = icon.position;
        Vector3 endPos = targetIcon.position;
        Image img = icon.GetComponent<Image>();
        Color startColor = img != null ? img.color : Color.white;

        float t = 0f;
        while (t < flyDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / flyDuration);
            float eased = flyEase.Evaluate(p);

            Vector3 pos = Vector3.Lerp(startPos, endPos, eased);
            pos.y += arcHeight * Mathf.Sin(p * Mathf.PI);
            icon.position = pos;

            icon.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, eased);

            if (img != null && p > 0.7f)
            {
                float fadeP = (p - 0.7f) / 0.3f;
                img.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), fadeP);
            }

            yield return null;
        }

        onArrived?.Invoke();
        Destroy(icon.gameObject);
    }

    private IEnumerator ScaleTo(RectTransform rt, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float scale = Mathf.Lerp(from, to, p);
            rt.localScale = Vector3.one * scale;
            yield return null;
        }
        rt.localScale = Vector3.one * to;
    }

    private IEnumerator PunchScale(RectTransform rt, float punchAmount, float duration)
    {
        Vector3 originalScale = rt.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float damper = Mathf.Exp(-p * 6f);
            float wave = Mathf.Sin(p * Mathf.PI * 4f) * punchAmount * damper;
            rt.localScale = originalScale * (1f + wave);
            yield return null;
        }
        rt.localScale = originalScale;
    }
}