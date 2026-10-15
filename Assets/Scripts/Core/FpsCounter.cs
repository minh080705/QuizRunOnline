using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bộ đếm FPS hiện ở góc trên trái màn hình để đo thật trên điện thoại. Tự tạo khi vào game (không cần gắn vào scene nào)
/// và CHỈ chạy trong Editor hoặc Development Build (Debug.isDebugBuild), bản phát hành sẽ không có.
///
/// Hiện FPS trung bình trong 0,5 giây gần nhất, thời gian trung bình mỗi frame và frame chậm nhất trong khoảng đó.
/// Lấy mẫu bằng Time.unscaledDeltaTime nên đúng cả khi game bị đổi timeScale.
///
/// Dùng uGUI (Canvas + Text) thay vì OnGUI: IMGUI tự cấp phát vài trăm byte mỗi frame (GUIUtility.BeginGUI) nên làm nhiễu
/// số đo GC Alloc trong Profiler. Chữ chỉ đổi mỗi 0,5 giây.
/// </summary>
public class FpsCounter : MonoBehaviour
{
    private const float Interval = 0.5f;

    private float elapsed;
    private int frames;
    private float worstFrame;

    private Text text;
    private RectTransform textRect;
    private Vector2Int lastScreenSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Debug.isDebugBuild) return;
        if (FindObjectOfType<FpsCounter>() != null) return;

        var go = new GameObject("FpsCounter");
        DontDestroyOnLoad(go);
        go.AddComponent<FpsCounter>();
    }

    private void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // luôn nằm trên cùng

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(transform, false);
        textRect = (RectTransform)textGo.transform;
        textRect.pivot = new Vector2(0f, 1f);

        text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontStyle = FontStyle.Bold;
        text.color = Color.yellow;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false; // không chặn chạm vào game
        text.text = "FPS ...";

        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        Layout();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        frames++;
        if (dt > worstFrame) worstFrame = dt;

        if (elapsed < Interval) return;

        float fps = frames / elapsed;
        float avgMs = elapsed / frames * 1000f;
        text.text = string.Format("{0:0} FPS | {1:0.0} ms | worst {2:0} ms", fps, avgMs, worstFrame * 1000f);

        elapsed = 0f;
        frames = 0;
        worstFrame = 0f;

        // Xoay màn hình / đổi độ phân giải thì sắp xếp lại (kiểm tra thưa, mỗi 0,5 giây).
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            Layout();
    }

    // Đặt nhãn ở góc trên trái trong vùng an toàn (tránh tai thỏ/camera đục lỗ), cỡ chữ theo chiều cao màn hình.
    private void Layout()
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        text.fontSize = Mathf.Max(14, Mathf.RoundToInt(Screen.height * 0.022f));

        Rect safe = Screen.safeArea;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(safe.x + 12f, -(Screen.height - (safe.y + safe.height)) - 12f);
        textRect.sizeDelta = new Vector2(Screen.width * 0.9f, text.fontSize * 1.8f);
    }
}
