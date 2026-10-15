using System.Collections.Generic;
using Fusion;
using UnityEngine;
using TMPro;

public class RoomListUI : MonoBehaviour
{
    public static RoomListUI Instance;

    [SerializeField] private Transform contentParent;
    [SerializeField] private RoomListItem itemPrefab;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField maxPlayersInput;
    [SerializeField] private GameObject roomPanel;

    [Tooltip("Dòng \"Chưa có phòng nào...\" (tùy chọn). Chỉ hiện khi đã nhận danh sách từ server mà không có phòng mở.")]
    [SerializeField] private GameObject emptyText;
    private List<RoomListItem> _spawnedItems = new List<RoomListItem>();

    void Awake()
    {
        Instance = this;
        if (emptyText != null) emptyText.SetActive(false); // chưa nhận danh sách thì chưa kết luận là "không có phòng"
    }

    public void RefreshList(List<SessionInfo> sessions)
    {
        foreach (var item in _spawnedItems) Destroy(item.gameObject);
        _spawnedItems.Clear();

        foreach (var session in sessions)
        {
            if (!session.IsOpen || !session.IsVisible) continue;

            var item = Instantiate(itemPrefab, contentParent);
            item.Setup(session.Name, session.PlayerCount, session.MaxPlayers, OnClickJoin);
            _spawnedItems.Add(item);
        }

        if (emptyText != null) emptyText.SetActive(_spawnedItems.Count == 0);
    }

    /// <summary>Quay lại màn danh sách phòng (sau khi thoát phòng): tắt panel phòng, xóa danh sách cũ chờ cập nhật mới.</summary>
    public void ShowRoomList()
    {
        roomPanel.SetActive(false);
        gameObject.SetActive(true);
        RefreshList(new List<SessionInfo>());
        if (emptyText != null) emptyText.SetActive(false); // danh sách trống ở đây chỉ là đang chờ server, không phải hết phòng
    }

    private async void OnClickJoin(string roomName)
    {
        bool success = await LobbyManager.Instance.JoinRoom(roomName);
        if (success)
        {
            gameObject.SetActive(false);
            roomPanel.SetActive(true); 
        }
    }
    public async void OnClickCreateRoom()
    {
        string roomName = string.IsNullOrEmpty(roomNameInput.text)
            ? "Room_" + Random.Range(1000, 9999)
            : roomNameInput.text;

        int maxPlayers = int.TryParse(maxPlayersInput.text, out int val) ? val : 4;

        bool success = await LobbyManager.Instance.CreateRoom(roomName, maxPlayers);
        if (success)
        {
            gameObject.SetActive(false);
            roomPanel.SetActive(true); 
        }
    }
}