using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chạy trên MỌI client: đọc SpawnEntry do Master sinh (ObstacleSpawnPointGenerator) rồi dựng object CỤC BỘ
/// (không phải NetworkObject) quanh người chơi local: vật cản, mẫu coin (bung từng đồng bằng CoinPatternBuilder),
/// buff, câu hỏi. Mỗi người nhặt bản của chính mình. Entry bị vô hiệu hoặc ra khỏi phạm vi thì dọn hết object con.
///
/// Không dùng object pool: prefab Coin có SimpleGemsAnim chụp vị trí ở Start() rồi tự ghi đè vị trí mỗi frame,
/// tái sử dụng object sẽ làm coin nhảy về chỗ cũ.
/// </summary>
public class LocalObstacleSpawner : MonoBehaviour
{
    [SerializeField] private ObstacleSpawnPointGenerator generator;
    [Tooltip("Cùng asset SpawnProfile với generator (thư viện prefab, thông số mẫu coin).")]
    [SerializeField] private SpawnProfile profile;
    [SerializeField] private float spawnRadius = 180f; // chỉ spawn trong bán kính này quanh player

    private Transform localPlayer;
    private readonly Dictionary<int, List<GameObject>> spawned = new Dictionary<int, List<GameObject>>();
    private readonly HashSet<int> shouldExistIds = new HashSet<int>();
    private readonly List<int> toRemove = new List<int>();
    private readonly List<Vector3> coinPositions = new List<Vector3>();

    void OnEnable()
    {
        PlayerContext.OnLocalPlayerReady += HandlePlayerReady;
    }

    void OnDisable()
    {
        PlayerContext.OnLocalPlayerReady -= HandlePlayerReady;
    }

    void HandlePlayerReady(Transform player)
    {
        localPlayer = player;
    }

    void Update()
    {
        if (localPlayer == null || profile == null) return;
        if (generator == null || generator.Object == null || !generator.Object.IsValid) return;

        var entries = generator.Entries;
        float playerZ = localPlayer.position.z;
        shouldExistIds.Clear();

        for (int i = 0; i < entries.Length; i++)
        {
            SpawnEntry entry = entries[i];
            if (!entry.Active) continue;

            // Mẫu dài (hàng coin) tính theo tâm của nó, nới phạm vi thêm nửa chiều dài.
            float centerZ = entry.Z + entry.Length * 0.5f;
            if (Mathf.Abs(centerZ - playerZ) > spawnRadius + entry.Length * 0.5f) continue;

            shouldExistIds.Add(entry.Id);

            if (!spawned.ContainsKey(entry.Id))
            {
                var objects = new List<GameObject>(4);
                SpawnEntryObjects(entry, objects);
                spawned[entry.Id] = objects;
            }
        }

        // Dọn mục không còn nên tồn tại (đã bị Master vô hiệu hoá, hoặc đã ra khỏi bán kính)
        toRemove.Clear();
        foreach (var kvp in spawned)
        {
            if (!shouldExistIds.Contains(kvp.Key)) toRemove.Add(kvp.Key);
        }
        foreach (int id in toRemove)
        {
            foreach (GameObject go in spawned[id])
            {
                if (go != null) Destroy(go); // coin đã nhặt thì đã bị Destroy, bỏ qua
            }
            spawned.Remove(id);
        }
    }

    private void SpawnEntryObjects(SpawnEntry entry, List<GameObject> objects)
    {
        switch ((SpawnKind)entry.Kind)
        {
            case SpawnKind.Obstacle:
                if (profile.obstacles != null && entry.Variant < profile.obstacles.Length)
                    Spawn(profile.obstacles[entry.Variant].prefab, entry.X, entry.Z, 0f, objects);
                break;

            case SpawnKind.Coin:
                if (profile.coinPrefab == null) break;
                CoinPatternBuilder.Build(entry, profile, coinPositions);
                foreach (Vector3 c in coinPositions)
                    Spawn(profile.coinPrefab, c.x, c.z, c.y, objects);
                break;

            case SpawnKind.Buff:
                if (profile.buffPrefabs != null && entry.Variant < profile.buffPrefabs.Length)
                    Spawn(profile.buffPrefabs[entry.Variant], entry.X, entry.Z, 0f, objects);
                break;

            case SpawnKind.Question:
                if (profile.questionPrefabs != null && entry.Variant < profile.questionPrefabs.Length)
                    Spawn(profile.questionPrefabs[entry.Variant], entry.X, entry.Z, 0f, objects);
                break;
        }
    }

    // Độ cao gốc lấy từ prefab (như bản cũ), cộng thêm yOffset của mẫu (coin trên không).
    private static void Spawn(GameObject prefab, float x, float z, float yOffset, List<GameObject> objects)
    {
        if (prefab == null) return;

        Vector3 pos = new Vector3(x, prefab.transform.position.y + yOffset, z);
        objects.Add(Instantiate(prefab, pos, prefab.transform.rotation));
    }
}
