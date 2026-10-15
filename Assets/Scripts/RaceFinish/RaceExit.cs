using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Thoát khỏi RaceScene về LobbyScene. Dùng chung cho nút thoát trận (LeaveRoomButton, có hộp xác nhận)
/// và nút về phòng trên bảng tổng kết (RaceSummaryUI, không hỏi lại vì trận đã xong).
/// </summary>
public static class RaceExit
{
    private static bool leaving;

    /// <summary>Lưu xu, reset bộ đếm xu ván này, rời phòng (tạo lại runner + session lobby) rồi về LobbyScene.</summary>
    public static async Task LeaveToLobbyAsync()
    {
        if (leaving) return; // chặn bấm liên tục
        leaving = true;

        try
        {
            // Lưu coin kiếm được (nếu chưa lưu) rồi reset bộ đếm để ván sau bắt đầu từ 0.
            if (CoinCalculator.Instance != null)
            {
                await CoinCalculator.Instance.FlushToProfileAsync();
                CoinCalculator.Instance.ResetSession();
            }

            if (LobbyManager.Instance != null)
            {
                // Rời trận và tạo lại runner + session lobby để LobbyScene có danh sách phòng.
                await LobbyManager.Instance.LeaveRoomAsync();
            }
            else
            {
                Debug.LogWarning("[RaceExit] Không có LobbyManager — bỏ qua bước dọn kết nối!");
            }

            SceneManager.LoadScene(SceneNames.Lobby);
        }
        finally
        {
            leaving = false;
        }
    }
}
