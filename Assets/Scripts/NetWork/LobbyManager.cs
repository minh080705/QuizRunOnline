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
        // LobbyManager sống qua các scene (RaceScene cần Runner). Quay lại LobbyScene mà bản cũ còn thì bỏ bản mới.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    async void Start()
    {
        // Bản trùng đã bị Destroy trong Awake: không tạo runner thứ hai.
        if (Instance != this) return;

        // Chưa đăng nhập: AuthGate đang đẩy về AccountScene, không kết nối và không giữ lại manager này.
        if (AuthGate.Redirecting)
        {
            Destroy(gameObject);
            return;
        }

        await ConnectToLobbyAsync();
    }

    /// <summary>Tạo runner mới và vào session lobby để nhận danh sách phòng.</summary>
    private async Task ConnectToLobbyAsync()
    {
        _runner = Instantiate(runnerPrefab);
        DontDestroyOnLoad(_runner.gameObject);
        _runner.AddCallbacks(this);

        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        if (!result.Ok)
            Debug.LogError($"Join Lobby thất bại: {result.ShutdownReason}");
    }

    private async Task ShutdownRunnerAsync()
    {
        if (_runner == null) return;

        NetworkRunner old = _runner;
        _runner = null;

        old.RemoveCallbacks(this); // tránh OnPlayerLeft/thông báo "đối thủ thoát" cho chính mình
        if (old.IsRunning)
            await old.Shutdown();

        if (old != null) Destroy(old.gameObject);
    }

    /// <summary>Rời phòng/trận hiện tại và quay lại sảnh (thấy danh sách phòng như lúc mới vào Lobby).</summary>
    public async Task LeaveRoomAsync()
    {
        await ShutdownRunnerAsync();
        PlayerContext.Instance?.Clear(); // bỏ tham chiếu player của ván vừa rời
        CurrentSessions = new List<SessionInfo>();
        await ConnectToLobbyAsync();
    }

    /// <summary>Dọn toàn bộ kết nối rồi tự hủy (dùng khi quay về AccountScene). Lobby mới sẽ tạo LobbyManager sạch.</summary>
    public async Task TeardownAsync()
    {
        await ShutdownRunnerAsync();
        Destroy(gameObject);
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
        UI_Notification.Instance?.ShowNetworkMessage("1 đối thủ đã thoát trận đấu!");
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