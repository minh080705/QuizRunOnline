using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class LeaveRoomButton : MonoBehaviour
{
    [SerializeField] private NetworkRunner runner; // kéo thả NetworkRunner trong scene vào đây
    [SerializeField] private string menuSceneName = "LobbyScene"; // tên scene menu để quay về

    public void OnLeaveButtonClicked()
    {
        UI_ConfirmDialog.Instance.Show(
            message: "Bạn có chắc muốn thoát trận đấu?",
            onConfirm: async () => await LeaveRoom(),
            onCancel: () => { }
        );
    }

    private async System.Threading.Tasks.Task LeaveRoom()
    {
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();

        if (runner != null && runner.IsRunning)
        {
 
            await runner.Shutdown();
            
        }
        else
        {
            Debug.LogWarning("[LeaveRoom] No running NetworkRunner found — Shutdown was skipped!");
        }

        SceneManager.LoadScene(menuSceneName);
    }
}