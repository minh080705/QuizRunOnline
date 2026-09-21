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
    private List<RoomListItem> _spawnedItems = new List<RoomListItem>();

    void Awake() => Instance = this;

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