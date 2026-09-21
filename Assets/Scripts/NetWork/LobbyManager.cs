using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static LobbyManager Instance;

    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkPrefabRef roomManagerPrefab; 
    private NetworkRunner _runner;
    [SerializeField] private NetworkPrefabRef playerDataPrefab;
    public List<SessionInfo> CurrentSessions { get; private set; } = new List<SessionInfo>();

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        _runner = Instantiate(runnerPrefab);
        DontDestroyOnLoad(_runner.gameObject); // ← thêm dòng này
        _runner.AddCallbacks(this);

        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        if (!result.Ok)
            Debug.LogError($"Join Lobby thất bại: {result.ShutdownReason}");
    }

    // Thêm property public để RaceScenePlayerSpawner lấy runner an toàn hơn FindObjectOfType
    public NetworkRunner Runner => _runner;

    public async Task<bool> CreateRoom(string roomName, int maxPlayers)
    {
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError($"Tạo phòng thất bại: {result.ShutdownReason}");
            return false;
        }

        _runner.Spawn(roomManagerPrefab);
        _runner.Spawn(playerDataPrefab, inputAuthority: _runner.LocalPlayer); // ← tự spawn cho chính mình
        return true;
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} đã rời phòng.");
        UI_Notification.Instance.ShowNetworkMessage("1 đối thủ đã thoát trận đấu!");
        // Có thể thêm logic: tự động thắng, quay về menu, v.v.
    }
    public async Task<bool> JoinRoom(string roomName)
    {
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError($"Join phòng thất bại: {result.ShutdownReason}");
            return false;
        }

        _runner.Spawn(playerDataPrefab, inputAuthority: _runner.LocalPlayer); // ← tự spawn cho chính mình
        return true;
    }

    // ĐANG DÙNG - giữ nguyên logic
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[LobbyManager] OnSessionListUpdated, sessions={sessionList.Count}");
        CurrentSessions = sessionList;
        RoomListUI.Instance?.RefreshList(sessionList);
    }

    // Các callback dưới đây KHÔNG dùng ở bước này -> để trống, KHÔNG throw exception
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}