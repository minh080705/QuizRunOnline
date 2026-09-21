using Fusion;
using UnityEngine;

public struct ObstacleSpawnPoint : INetworkStruct
{
    public int Id;
    public int ObstacleIndex;
    public Vector3 Position;
    public NetworkBool Active;
}

public class ObstacleSpawnPointGenerator : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float baseSpawnInterval = 15f;   // khoảng cách gốc giữa 2 obstacle, ở accelerationRate gốc
    [SerializeField] private float baseAccelerationRate = 1f; // giá trị accelerationRate tham chiếu để tính tỉ lệ
    [SerializeField] private float spawnAheadDistance = 100f; // sinh trước leader bao xa
    [SerializeField] private int obstacleTypeCount = 5;       // số loại obstacle (khớp số phần tử trong list ở client)
    [SerializeField] private float[] laneX = new float[] { -2.5f, 0f, 2.5f }; // vị trí X của 3 đường ray
    [Header("Obstacle Spawn Weights")]
    [Tooltip("Tỉ lệ xuất hiện tương đối cho từng loại obstacle, index khớp với obstaclePrefabs bên LocalObstacleSpawner.")]
    [SerializeField] private float[] obstacleWeights = new float[] { 1f, 1f, 1f, 1f, 1f };
    private const int Capacity = 64;
    [Header("Settings")]
    [SerializeField] private float despawnBufferDistance = 20f; // khoảng đệm phía sau trước khi thật sự xóa
    [Networked, Capacity(64)] public NetworkArray<ObstacleSpawnPoint> SpawnPoints => default;
    [Networked] private float lastSpawnZ { get; set; }
    [Networked] private int nextId { get; set; }
    [Networked] private int lastLaneIndex { get; set; } = -1; // lane vừa spawn lần trước, để tránh trùng

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return; // chỉ Master tính toán, client khác chỉ đọc

        var leader = RacePlayersRegistry.Instance.GetLeadingPlayer();
        var trailer = RacePlayersRegistry.Instance.GetTrailingPlayer();
        if (leader == null || trailer == null) return;

        float currentInterval = GetSpawnInterval(leader);

        while (leader.transform.position.z + spawnAheadDistance > lastSpawnZ)
        {
            SpawnNewPoint(currentInterval);
        }

        for (int i = 0; i < Capacity; i++)
        {
            var point = SpawnPoints[i];
            if (point.Active && point.Position.z < trailer.transform.position.z - despawnBufferDistance)
            {
                point.Active = false;
                SpawnPoints.Set(i, point);
            }
        }
    }

    private float GetSpawnInterval(PlayerStats leader)
    {
        // khoảng cách tăng theo tỉ lệ với accelerationRate hiện tại so với mốc gốc
        float ratio = baseAccelerationRate > 0f ? leader.AccelerationRate / baseAccelerationRate : 1f;
        return baseSpawnInterval * ratio;
    }

    private void SpawnNewPoint(float interval)
    {
        lastSpawnZ += interval;
        int slot = FindFreeSlot();
        if (slot == -1) return; // hết chỗ trống, tăng Capacity nếu cần

        int newLaneIndex = PickLaneDifferentFromLast();
        float x = laneX[newLaneIndex];
        lastLaneIndex = newLaneIndex;

        SpawnPoints.Set(slot, new ObstacleSpawnPoint
        {
            Id = nextId++,
            ObstacleIndex = PickWeightedObstacleIndex(), // thay vì Random.Range(0, obstacleTypeCount)
            Position = new Vector3(x, 0f, lastSpawnZ),
            Active = true
        });
    }

    private int PickLaneDifferentFromLast()
    {
        if (lastLaneIndex == -1) return Random.Range(0, laneX.Length); // lần đầu, chọn tự do

        int newIndex;
        do
        {
            newIndex = Random.Range(0, laneX.Length);
        } while (newIndex == lastLaneIndex); // đảm bảo khác lane trước, luôn còn ít nhất 1 lane trống liên tiếp

        return newIndex;
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (!SpawnPoints[i].Active) return i;
        }
        return -1;
    }
    private int PickWeightedObstacleIndex()
    {
        // fallback nếu quên set weight hoặc set sai kích thước
        if (obstacleWeights == null || obstacleWeights.Length != obstacleTypeCount)
        {
            return Random.Range(0, obstacleTypeCount);
        }

        float total = 0f;
        for (int i = 0; i < obstacleWeights.Length; i++)
            total += Mathf.Max(0f, obstacleWeights[i]);

        if (total <= 0f) return Random.Range(0, obstacleTypeCount);

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < obstacleWeights.Length; i++)
        {
            cumulative += Mathf.Max(0f, obstacleWeights[i]);
            if (roll <= cumulative) return i;
        }

        return obstacleWeights.Length - 1; // safety fallback
    }
    private void OnValidate()
    {
        if (obstacleTypeCount < 1) obstacleTypeCount = 1;

        if (obstacleWeights == null || obstacleWeights.Length != obstacleTypeCount)
        {
            float[] resized = new float[obstacleTypeCount];
            for (int i = 0; i < resized.Length; i++)
            {
                resized[i] = (obstacleWeights != null && i < obstacleWeights.Length)
                    ? obstacleWeights[i]
                    : 1f; // giá trị mặc định cho phần tử mới
            }
            obstacleWeights = resized;
        }
    }
}