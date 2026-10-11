using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_SIGNIN
using Google;
#endif

/// <summary>
/// Đăng nhập Firebase. Trên Android dùng Google Sign-In (cần plugin + define GOOGLE_SIGNIN).
/// Trong Editor (hoặc khi chưa có plugin) dùng đăng nhập ẩn danh để test luồng dữ liệu.
/// </summary>
public class AuthService : MonoBehaviour
{
    public static AuthService Instance { get; private set; }

    [Tooltip("Web client ID (OAuth 2.0, loại Web) lấy trong Firebase > Authentication > Google.")]
    [SerializeField] private string webClientId;

    private FirebaseAuth auth;

    // Chế độ cục bộ (BackendMode.UseFirebase == false): chỉ giữ id trong bộ nhớ.
    private string localUserId;

    public string UserId => BackendMode.UseFirebase ? auth?.CurrentUser?.UserId : localUserId;
    public string DisplayName => BackendMode.UseFirebase ? auth?.CurrentUser?.DisplayName : null;
    public string Email => BackendMode.UseFirebase ? auth?.CurrentUser?.Email : null;
    public bool IsSignedIn => !string.IsNullOrEmpty(UserId);

    public event Action OnSignedIn;
    public event Action OnSignedOut;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async Task<bool> EnsureInitAsync()
    {
        if (auth != null) return true;

        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"[Auth] Firebase dependency lỗi: {status}");
            return false;
        }

        auth = FirebaseAuth.DefaultInstance;
        return true;
    }

    /// <summary>Dùng session đã lưu nếu có, trả về true khi đã đăng nhập.</summary>
    public async Task<bool> TryRestoreSessionAsync()
    {
        if (!BackendMode.UseFirebase) return IsSignedIn;

        if (!await EnsureInitAsync()) return false;
        if (auth.CurrentUser == null) return false;

        OnSignedIn?.Invoke();
        return true;
    }

    public async Task<bool> SignInAsync()
    {
        if (!BackendMode.UseFirebase)
        {
            localUserId = "local-" + Guid.NewGuid().ToString("N");
            OnSignedIn?.Invoke();
            return true;
        }

        if (!await EnsureInitAsync()) return false;

        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_SIGNIN
            if (string.IsNullOrEmpty(webClientId))
            {
                Debug.LogError("[Auth] Chưa gán Web client ID cho AuthService.");
                return false;
            }

            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                UseGameSignIn = false,
                RequestIdToken = true,
                RequestEmail = true,
            };

            GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
            await auth.SignInWithCredentialAsync(credential);
#elif UNITY_EDITOR
            // Editor: Google Sign-In không chạy được, dùng ẩn danh (bật Anonymous trong Firebase > Authentication).
            await auth.SignInAnonymouslyAsync();
#else
            Debug.LogError("[Auth] Build này chưa có Google Sign-In (cần Android + plugin + define GOOGLE_SIGNIN).");
            return false;
#endif
        }
#if UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_SIGNIN
        catch (GoogleSignIn.SignInException e)
        {
            // Người dùng hủy hộp chọn tài khoản cũng rơi vào đây: không coi là lỗi nghiêm trọng.
            Debug.LogWarning($"[Auth] Google Sign-In không hoàn tất: {e.Status} - {e.Message}");
            return false;
        }
#endif
        catch (Exception e)
        {
            Debug.LogError($"[Auth] Đăng nhập thất bại: {e}");
            return false;
        }

        OnSignedIn?.Invoke();
        return true;
    }

    public void SignOut()
    {
        if (!BackendMode.UseFirebase)
        {
            localUserId = null;
            OnSignedOut?.Invoke();
            return;
        }

        if (auth == null) return;
        auth.SignOut();
#if UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_SIGNIN
        GoogleSignIn.DefaultInstance.SignOut();
#endif
        OnSignedOut?.Invoke();
    }

    /// <summary>Xóa tài khoản Auth. Có thể lỗi nếu phiên đăng nhập quá cũ (cần đăng nhập lại).</summary>
    public async Task<bool> DeleteAccountAsync()
    {
        if (!BackendMode.UseFirebase)
        {
            localUserId = null;
            OnSignedOut?.Invoke();
            return true;
        }

        if (auth?.CurrentUser == null) return false;
        try
        {
            await auth.CurrentUser.DeleteAsync();
#if UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_SIGNIN
            GoogleSignIn.DefaultInstance.Disconnect(); // thu hồi quyền đã cấp cho app
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auth] Xóa tài khoản thất bại (thử đăng nhập lại rồi xóa): {e}");
            return false;
        }

        OnSignedOut?.Invoke();
        return true;
    }
}
