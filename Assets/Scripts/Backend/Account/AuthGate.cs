using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đặt trong LobbyScene: chưa đăng nhập (sau khi đăng xuất, hoặc mở thẳng scene) thì chuyển về AccountScene.
/// Chạy trước các script khác (execution order thấp) để LobbyManager không kịp kết nối mạng.
/// </summary>
[DefaultExecutionOrder(-100)]
public class AuthGate : MonoBehaviour
{
    [Tooltip("Trong Editor cho phép mở thẳng LobbyScene mà không cần đăng nhập (để test nhanh).")]
    [SerializeField] private bool bypassInEditor = true;

    /// <summary>true nếu đã có phiên đăng nhập và hồ sơ trong bộ nhớ.</summary>
    public static bool IsLoggedIn =>
        AuthService.Instance != null && AuthService.Instance.IsSignedIn &&
        UserProfileService.Instance != null && UserProfileService.Instance.Current != null;

    /// <summary>true trong lúc đang bị đẩy về AccountScene; LobbyManager dùng để không kết nối mạng.</summary>
    public static bool Redirecting { get; private set; }

    private void Awake()
    {
        Redirecting = false;
#if UNITY_EDITOR
        if (bypassInEditor) return;
#endif
        if (IsLoggedIn) return;

        Redirecting = true;
        SceneManager.LoadScene(SceneNames.Account);
    }
}
