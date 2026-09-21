using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RoomPanelUI : MonoBehaviour
{
    public static RoomPanelUI Instance;

    [SerializeField] private Transform playerListParent;
    [SerializeField] private PlayerRowUI rowPrefab;

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
    }

    public void OnClickReady()
    {
        if (_myData == null) return;
        _myData.SetReady(!_myData.IsReady);
    }
}