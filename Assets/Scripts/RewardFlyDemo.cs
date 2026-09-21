using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DEMO: Hiệu ứng icon phần thưởng xuất hiện tại 1 vị trí -> bay về vị trí đích -> biến mất.
/// Không cần cài thêm thư viện nào (DOTween, ...) - chỉ dùng Coroutine + AnimationCurve.
///
/// CÁCH SETUP SCENE ĐỂ CHẠY THỬ:
/// 1. Tạo Canvas (Screen Space - Overlay).
/// 2. Trong Canvas, tạo 1 Image (đặt tên "RewardIcon") - đây là icon phần thưởng gốc,
///    kéo 1 sprite bất kỳ (vd icon xu, item...) vào, rồi TẮT (SetActive false) - script sẽ tự
///    Instantiate bản sao khi cần, ảnh gốc chỉ dùng làm prefab tham chiếu.
/// 3. Tạo 1 Image/RectTransform khác đặt tên "TargetIcon" (vd icon túi đồ ở góc màn hình) -
///    đây là nơi item sẽ bay đến.
/// 4. Tạo 1 Button đặt tên "TestButton" để bấm test.
/// 5. Tạo Empty GameObject đặt tên "RewardManager", gắn script này vào,
///    kéo RewardIcon, TargetIcon, Canvas vào các ô tương ứng trong Inspector.
/// 6. Trong OnClick() của TestButton, kéo RewardManager vào, chọn hàm
///    RewardFlyDemo -> TestReward().
/// 7. Bấm Play, bấm nút Test -> icon sẽ xuất hiện giữa màn hình và bay về TargetIcon.
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

    /// <summary>
    /// Gọi hàm này để test (gắn vào OnClick của Button).
    /// Icon sẽ xuất hiện tại giữa màn hình.
    /// </summary>
    public void TestReward()
    {
        Vector3 centerScreen = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        PlayRewardEffect(centerScreen, () =>
        {
            // Rung nhẹ icon đích để báo hiệu đã nhận
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
        icon.position = startScreenPos;
        icon.localScale = Vector3.zero;

        StartCoroutine(RewardRoutine(icon, onArrived));
    }

    private IEnumerator RewardRoutine(RectTransform icon, Action onArrived)
    {
        // ----- BƯỚC 1: Pop-in xuất hiện (scale 0 -> hơi lố -> về 1) -----
        yield return ScaleTo(icon, 0f, 1.2f, popDuration * 0.7f);
        yield return ScaleTo(icon, 1.2f, 1f, popDuration * 0.3f);

        // ----- BƯỚC 2: Đứng yên 1 chút để người chơi kịp nhìn thấy -----
        yield return new WaitForSeconds(holdDuration);

        // ----- BƯỚC 3: Bay về đích theo đường cong, đồng thời scale nhỏ dần -----
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

            // Nội suy vị trí thẳng
            Vector3 pos = Vector3.Lerp(startPos, endPos, eased);
            // Cộng thêm độ cao vòng cung (parabol: cao nhất ở giữa đường bay)
            pos.y += arcHeight * Mathf.Sin(p * Mathf.PI);
            icon.position = pos;

            // Scale nhỏ dần lại khi bay (tạo cảm giác "hút vào" đích)
            icon.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, eased);

            // Mờ dần nhẹ ở đoạn cuối đường bay
            if (img != null && p > 0.7f)
            {
                float fadeP = (p - 0.7f) / 0.3f;
                img.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), fadeP);
            }

            yield return null;
        }

        // ----- BƯỚC 4: Đến đích -> báo hiệu -> huỷ icon -----
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
            // damped sine wave: nảy rồi tắt dần
            float damper = Mathf.Exp(-p * 6f);
            float wave = Mathf.Sin(p * Mathf.PI * 4f) * punchAmount * damper;
            rt.localScale = originalScale * (1f + wave);
            yield return null;
        }
        rt.localScale = originalScale;
    }
}