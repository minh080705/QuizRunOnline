using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MenuScene: màn hình đầu tiên sau khi đăng nhập. Bốn nút: Play Online (vào Lobby), Account, Shop, Thoát game.
/// AccountScene tự chuyển sang đây khi đã đăng nhập; AuthGate trong scene đẩy về AccountScene nếu chưa đăng nhập.
/// Tùy chọn: hiện tên người chơi và số xu (đọc từ hồ sơ UserProfileService).
/// </summary>
public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button playOnlineButton;
    [SerializeField] private Button accountButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button quitButton;

    [Header("Thông tin người chơi (tùy chọn, bỏ trống thì bỏ qua)")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text coinText;

    private void Start()
    {
        if (playOnlineButton != null) playOnlineButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Lobby));
        if (accountButton != null) accountButton.onClick.AddListener(OpenAccount);
        if (shopButton != null) shopButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Shop));
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        UserProfileService profiles = UserProfileService.Instance;
        RefreshProfile(profiles != null ? profiles.Current : null);
    }

    private void OnEnable()
    {
        if (UserProfileService.Instance != null)
            UserProfileService.Instance.OnProfileChanged += RefreshProfile;
    }

    private void OnDisable()
    {
        if (UserProfileService.Instance != null)
            UserProfileService.Instance.OnProfileChanged -= RefreshProfile;
    }

    private void RefreshProfile(UserProfile profile)
    {
        if (profile == null) return;

        if (playerNameText != null) playerNameText.text = profile.characterName;
        if (coinText != null) coinText.text = $"{profile.coins}";
    }

    private void OpenAccount()
    {
        // Báo cho AccountScene ở lại màn hình hồ sơ, không tự nhảy ngược về menu.
        AccountUI.StayOnAccountScreen = true;
        SceneManager.LoadScene(SceneNames.Account);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // trong Editor chỉ dừng Play
#else
        Application.Quit();
#endif
    }
}
