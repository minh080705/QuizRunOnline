using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class RoomPanelUI : MonoBehaviour
{
    public static RoomPanelUI Instance;

    [SerializeField] private Transform playerListParent;
    [SerializeField] private PlayerRowUI rowPrefab;

    [Header("Tiêu đề phòng (tùy chọn)")]
    [SerializeField] private TMP_Text roomNameText;
    [Tooltip("Dạng 2/4: số người hiện có / tối đa của phòng.")]
    [SerializeField] private TMP_Text playerCountText;

    [Header("Nút Sẵn sàng (tùy chọn)")]
    [Tooltip("Nhãn của nút gọi OnClickReady: đổi giữa \"Sẵn sàng\" và \"Bỏ sẵn sàng\".")]
    [SerializeField] private TMP_Text readyButtonText;

    private Dictionary<PlayerRoomData, PlayerRowUI> _rowMap = new();
    private PlayerRoomData _myData;

    void Awake() => Instance = this;

    void Update()
    {
        RefreshPlayerList();
    }

    public void RefreshPlayerList()
    {
        var allPlayers = FindObjectsOfType<PlayerRoomData>();
        var currentSet = new HashSet<PlayerRoomData>(allPlayers);

        // Xóa hàng của người đã rời phòng
        var toRemove = new List<PlayerRoomData>();
        foreach (var kvp in _rowMap)
        {
            if (!currentSet.Contains(kvp.Key))
            {
                Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove) _rowMap.Remove(key);

        // Tạo hàng mới (nếu có), hoặc chỉ cập nhật hàng đã tồn tại
        foreach (var data in allPlayers)
        {
            bool isMine = data.Object.HasStateAuthority;
            if (isMine) _myData = data;

            if (!_rowMap.TryGetValue(data, out var row))
            {
                row = Instantiate(rowPrefab, playerListParent);
                row.Setup(data, isMine); // chỉ chạy 1 lần lúc tạo, gắn sự kiện nút bấm ở đây
                _rowMap[data] = row;
            }

            row.Refresh(); // chạy mỗi frame, chỉ cập nhật chữ hiển thị
        }

        RefreshHeader();
        RefreshReadyLabel();
    }

    // Tên phòng và số người lấy từ SessionInfo của runner đang chạy.
    private void RefreshHeader()
    {
        if (roomNameText == null && playerCountText == null) return;

        NetworkRunner runner = LobbyManager.Instance != null ? LobbyManager.Instance.Runner : null;
        if (runner == null || !runner.IsRunning) return;

        SessionInfo session = runner.SessionInfo;
        if (session == null || !session.IsValid) return;

        SetText(roomNameText, session.Name);
        SetText(playerCountText, $"{session.PlayerCount}/{session.MaxPlayers}");
    }

    private void RefreshReadyLabel()
    {
        if (readyButtonText == null) return;
        SetText(readyButtonText, _myData != null && _myData.IsReady ? "Bỏ sẵn sàng" : "Sẵn sàng");
    }

    // Chỉ gán khi đổi, vì hàm này chạy mỗi frame (gán cùng chuỗi vẫn làm TMP dựng lại mesh).
    private static void SetText(TMP_Text label, string value)
    {
        if (label != null && label.text != value) label.text = value;
    }

    public void OnClickReady()
    {
        if (_myData == null) return;
        _myData.SetReady(!_myData.IsReady);
    }
}