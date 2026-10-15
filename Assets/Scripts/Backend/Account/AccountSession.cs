using System.Threading.Tasks;
using UnityEngine.SceneManagement;

/// <summary>
/// Điểm duy nhất cho các thao tác chuyển phiên: về AccountScene và dọn dữ liệu khi đăng xuất.
/// </summary>
public static class AccountSession
{
    /// <summary>Lưu coin chưa ghi, dọn kết nối mạng rồi quay về AccountScene (vẫn giữ đăng nhập).</summary>
    public static async Task GoToAccountAsync()
    {
        if (CoinCalculator.Instance != null)
            await CoinCalculator.Instance.FlushToProfileAsync();

        if (LobbyManager.Instance != null)
            await LobbyManager.Instance.TeardownAsync();

        SceneManager.LoadScene(SceneNames.Account);
    }

    /// <summary>Gọi sau khi đăng xuất / xóa tài khoản: bỏ mọi dữ liệu của user cũ còn trong bộ nhớ.</summary>
    public static void ClearLocalState()
    {
        if (UserProfileService.Instance != null)
            UserProfileService.Instance.Clear();

        if (CoinCalculator.Instance != null)
            CoinCalculator.Instance.ResetSession();

        LocalPlayerSelection.CharacterIndex = 0;
        LocalPlayerSelection.SkinIndex = 0;
    }
}
