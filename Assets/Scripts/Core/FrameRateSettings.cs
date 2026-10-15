using UnityEngine;

/// <summary>
/// Đặt tốc độ khung hình cho bản chạy trên Android, tự chạy trước scene đầu tiên (không cần gắn vào scene nào).
///
/// Mức chất lượng Medium (mặc định của Android) bật VSync. Khi VSync bật mà frame lâu hơn 16,6 ms thì điện thoại bị ép xuống đúng
/// 30 FPS (frame phải chờ nhịp màn hình kế tiếp), và Application.targetFrameRate bị bỏ qua. Tắt VSync để targetFrameRate có tác dụng.
/// Chỉ áp dụng trên thiết bị Android thật, không đổi gì trong Editor hay bản Windows.
/// </summary>
public static class FrameRateSettings
{
    private const int TargetFps = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFps;
#endif
    }
}
