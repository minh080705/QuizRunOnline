using Fusion;
using UnityEngine;

/// <summary>
/// Spawn vạch đích (FinishLine prefab) một lần duy nhất, khi người dẫn đầu tới gần vị trí đích
/// (RaceTrackConfig.FinishZ, định sẵn từ đầu trận).
///
/// Là scene object có NetworkObject (giống ObstacleSpawnPointGenerator): trong Shared mode chỉ máy
/// có StateAuthority (Master) chạy logic spawn, object spawn ra được Fusion replicate cho mọi máy.
/// </summary>
public class FinnishLineSpawner : NetworkBehaviour
{
    [Header("Prefab vạch đích (phải có NetworkObject + FinishLine)")]
    [SerializeField] private NetworkPrefabRef finishLinePrefab;

    [Header("Khi nào spawn")]
    [Tooltip("Spawn khi người dẫn đầu còn cách vạch đích tối đa chừng này (mét). Đủ lớn để các máy khác kịp nhận object trước khi chạm.")]
    [SerializeField] private float spawnAheadDistance = 250f;

    [Header("Vị trí / hướng (Z lấy từ RaceTrackConfig.FinishZ)")]
    [SerializeField] private float finishX = 0f;
    [SerializeField] private float finishY = 0f;
    [SerializeField] private Vector3 finishEulerAngles = Vector3.zero;

    [Header("Podium (GameObject có sẵn trong RaceScene, kéo vào đây)")]
    [Tooltip("Podium được dời tới sát vạch đích ngay khi vạch đích spawn (trên mọi máy).")]
    [SerializeField] private GameObject podium;
    [Tooltip("Cộng vào vị trí vạch đích. +Y = phía trên, +Z = phía sau vạch (bên kia vạch, nơi người chơi chạy tới). " +
             "Mặc định: trên 30, sau 30. Đổi dấu Z nếu muốn đặt phía trước vạch.")]
    [SerializeField] private Vector3 podiumOffset = new Vector3(0f, 30f, 30f);

    // Lưu trên mạng: nếu Master rời phòng, Master mới sẽ không spawn lần thứ hai.
    [Networked] private NetworkBool FinishSpawned { get; set; }

    private bool warnedMissingConfig;

    /// <summary>Scene object có mặt trên mọi máy; FinishLine (prefab) dùng nó để đặt podium khi Spawned().</summary>
    public static FinnishLineSpawner Instance { get; private set; }

    public override void Spawned()
    {
        Instance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Dời podium tới (vị trí vạch đích + podiumOffset). Gọi từ FinishLine.Spawned() trên mọi máy.</summary>
    public void PlacePodium(Vector3 finishPosition)
    {
        if (podium == null) return;

        podium.transform.position = finishPosition + podiumOffset;
        if (!podium.activeSelf) podium.SetActive(true);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || FinishSpawned) return;

        RaceTrackConfig track = RaceTrackConfig.Instance;
        if (track == null)
        {
            if (!warnedMissingConfig)
            {
                warnedMissingConfig = true;
                Debug.LogError("[FinnishLineSpawner] Không có RaceTrackConfig trong scene, không biết spawn vạch đích ở đâu.");
            }
            return;
        }

        if (RacePlayersRegistry.Instance == null) return;

        PlayerStats leader = RacePlayersRegistry.Instance.GetLeadingPlayer();
        if (leader == null) return;

        if (leader.transform.position.z < track.FinishZ - spawnAheadDistance) return;

        SpawnFinishLine(track.FinishZ);
    }

    private void SpawnFinishLine(float finishZ)
    {
        if (!finishLinePrefab.IsValid)
        {
            Debug.LogError("[FinnishLineSpawner] Chưa gán prefab vạch đích (Board.001).");
            FinishSpawned = true; // tránh báo lỗi lặp mỗi tick
            return;
        }

        Vector3 position = new Vector3(finishX, finishY, finishZ);
        Quaternion rotation = Quaternion.Euler(finishEulerAngles);

        // Vị trí truyền vào Spawn chỉ áp dụng trên máy này; gửi kèm qua dữ liệu mạng của FinishLine
        // (onBeforeSpawned chạy trước khi object được replicate) để các máy khác đặt đúng chỗ.
        Runner.Spawn(
            finishLinePrefab,
            position,
            rotation,
            onBeforeSpawned: (spawnRunner, spawnedObject) =>
            {
                FinishLine line = spawnedObject.GetComponentInChildren<FinishLine>();
                if (line != null)
                    line.SetSpawnPose(position, rotation);
                else
                    Debug.LogError("[FinnishLineSpawner] Prefab vạch đích không có component FinishLine.");
            });
        FinishSpawned = true;

        Debug.Log($"[FinnishLineSpawner] Đã spawn vạch đích tại Z={finishZ}.");
    }
}
