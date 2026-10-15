using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;

    private string _roomName;
    private Action<string> _onJoin;

    public void Setup(string roomName, int current, int max, Action<string> onJoin)
    {
        _roomName = roomName;
        _onJoin = onJoin;
        roomNameText.text = roomName;
        playerCountText.text = $"{current}/{max}";
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => _onJoin(_roomName));
    }
}