using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nút điều hướng trong LobbyScene: "Tài khoản" (về AccountScene) và "Thoát phòng" (chỉ hiện khi đang ở trong phòng).
/// Gắn lên một GameObject luôn bật (không nằm dưới RoomListUI hay roomPanel vì các panel đó bị tắt/bật).
/// </summary>
public class LobbyNavigationUI : MonoBehaviour
{
    [SerializeField] private Button accountButton;
    [SerializeField] private Button leaveRoomButton;

    [Tooltip("Panel phòng chờ (cùng panel mà RoomListUI bật khi vào phòng).")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private RoomListUI roomListUI;

    private bool busy;

    private void Start()
    {
        accountButton.onClick.AddListener(OnAccountClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
    }

    private void Update()
    {
        // Nút thoát phòng chỉ có nghĩa khi đang ở trong phòng.
        leaveRoomButton.gameObject.SetActive(roomPanel.activeInHierarchy);
    }

    private void OnAccountClicked()
    {
        if (busy) return;

        if (roomPanel.activeInHierarchy && UI_ConfirmDialog.Instance != null)
        {
            UI_ConfirmDialog.Instance.Show(
                "Bạn sẽ rời phòng hiện tại và quay về màn hình tài khoản. Tiếp tục?",
                onConfirm: () => _ = GoToAccount());
        }
        else
        {
            _ = GoToAccount();
        }
    }

    private void OnLeaveRoomClicked()
    {
        if (busy) return;

        if (UI_ConfirmDialog.Instance != null)
        {
            UI_ConfirmDialog.Instance.Show(
                "Bạn có chắc muốn thoát phòng?",
                onConfirm: () => _ = LeaveRoom());
        }
        else
        {
            _ = LeaveRoom();
        }
    }

    private async System.Threading.Tasks.Task GoToAccount()
    {
        busy = true;
        await AccountSession.GoToAccountAsync();
    }

    private async System.Threading.Tasks.Task LeaveRoom()
    {
        busy = true;
        if (LobbyManager.Instance != null)
            await LobbyManager.Instance.LeaveRoomAsync();

        roomListUI.ShowRoomList();
        busy = false;
    }
}
