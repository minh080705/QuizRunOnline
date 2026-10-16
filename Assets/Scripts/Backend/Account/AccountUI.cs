using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI của AccountScene: đăng nhập Google, sửa tên, xem coin, đăng xuất.
/// Kéo thả các tham chiếu trong Inspector.
/// </summary>
public class AccountUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject profilePanel;

    [Header("Login")]
    [SerializeField] private Button signInButton;

    [Header("Profile")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button saveNameButton;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Button signOutButton;
    [SerializeField] private Button playButton;
    [Tooltip("Tùy chọn: nút 'Về menu' quay lại MenuScene.")]
    [SerializeField] private Button menuButton;

    [Header("Khác")]
    [SerializeField] private TMP_Text statusText;

    /// <summary>
    /// MenuScene đặt true trước khi mở AccountScene bằng nút "Account", để scene này ở lại cho người dùng xem/sửa hồ sơ.
    /// Bình thường (mở game, hoặc vừa đăng nhập xong) khi đã đăng nhập thì AccountScene tự chuyển sang MenuScene.
    /// </summary>
    public static bool StayOnAccountScreen;

    private async void Start()
    {
        signInButton.onClick.AddListener(OnSignInClicked);
        saveNameButton.onClick.AddListener(OnSaveNameClicked);
        signOutButton.onClick.AddListener(OnSignOutClicked);
        playButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Lobby));
        if (menuButton != null) menuButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Menu));

        UserProfileService.Instance.OnProfileChanged += Refresh;

        // Đọc rồi xóa cờ: chỉ có tác dụng cho đúng một lần mở AccountScene này.
        bool stay = StayOnAccountScreen;
        StayOnAccountScreen = false;

        // Đã đăng nhập và hồ sơ còn trong bộ nhớ (dịch vụ sống qua các scene): hiện luôn, không đọc lại Firestore.
        UserProfileService profiles = UserProfileService.Instance;
        if (AuthService.Instance.IsSignedIn && profiles.Current != null)
        {
            SetStatus("");
            if (stay)
                Refresh(profiles.Current);
            else
                SceneManager.LoadScene(SceneNames.Menu);
            return;
        }

        // Chưa biết đã đăng nhập chưa: ẩn cả hai panel (không bật panel đăng nhập) cho tới khi kiểm tra xong.
        HidePanels();
        SetStatus("Đang kiểm tra đăng nhập...");

        if (await AuthService.Instance.TryRestoreSessionAsync())
        {
            bool loaded = await LoadProfileAsync();
            if (loaded && !stay)
                SceneManager.LoadScene(SceneNames.Menu); // đã đăng nhập: vào thẳng menu
        }
        else
        {
            SetStatus("");
            ShowPanel(false); // kiểm tra xong, chưa đăng nhập: bây giờ mới hiện panel đăng nhập
        }
    }

    private void HidePanels()
    {
        loginPanel.SetActive(false);
        profilePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (UserProfileService.Instance != null)
            UserProfileService.Instance.OnProfileChanged -= Refresh;
    }

    private async void OnSignInClicked()
    {
        signInButton.interactable = false;
        SetStatus("Đang đăng nhập...");

        if (await AuthService.Instance.SignInAsync())
        {
            if (await LoadProfileAsync())
            {
                SceneManager.LoadScene(SceneNames.Menu); // vừa đăng nhập xong: vào menu
                return;
            }
        }
        else
        {
            SetStatus("Đăng nhập thất bại.");
        }

        signInButton.interactable = true;
    }

    /// <summary>Nạp hồ sơ; trả về false nếu không tải được.</summary>
    private async System.Threading.Tasks.Task<bool> LoadProfileAsync()
    {
        SetStatus("Đang tải hồ sơ...");
        var profile = await UserProfileService.Instance.LoadOrCreateAsync();
        if (profile == null)
        {
            SetStatus("Không tải được hồ sơ.");
            ShowPanel(false);
            return false;
        }
        SetStatus("");
        return true;
    }

    private async void OnSaveNameClicked()
    {
        bool ok = await UserProfileService.Instance.SetCharacterNameAsync(nameInput.text);
        SetStatus(ok ? "Đã lưu tên." : "Lưu tên thất bại.");
    }

    private void OnSignOutClicked()
    {
        AuthService.Instance.SignOut();
        AccountSession.ClearLocalState();
        ShowPanel(false);
        SetStatus("");
    }

    private void Refresh(UserProfile p)
    {
        if (p == null) { ShowPanel(false); return; }

        ShowPanel(true);
        if (!nameInput.isFocused) nameInput.text = p.characterName;
        coinText.text = $"Coin: {p.coins}";
    }

    private void ShowPanel(bool loggedIn)
    {
        loginPanel.SetActive(!loggedIn);
        profilePanel.SetActive(loggedIn);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
