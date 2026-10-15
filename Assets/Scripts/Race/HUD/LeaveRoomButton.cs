using UnityEngine;

public class LeaveRoomButton : MonoBehaviour
{
    public void OnLeaveButtonClicked()
    {
        UI_ConfirmDialog.Instance.Show(
            message: "Bạn có chắc muốn thoát trận đấu?",
            onConfirm: async () => await RaceExit.LeaveToLobbyAsync(),
            onCancel: () => { }
        );
    }
}
