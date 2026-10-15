using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chạy trên MỌI client: đọc SpawnEntry do Master sinh (ObstacleSpawnPointGenerator) rồi dựng object CỤC BỘ
/// (không phải NetworkObject) quanh "tâm thế giới" (WorldFocus): vật cản, mẫu coin (bung từng đồng bằng CoinPatternBuilder),
/// buff, câu hỏi. Tâm mặc định là nhân vật local; khi đang quan sát người khác (hết mạng / đã về đích) tâm là người đang xem,
/// nên quanh họ luôn có đủ vật. Mỗi người nhặt bản của chính mình. Entry bị vô hiệu hoặc ra khỏi phạm vi thì dọn hết object con.
///
/// Không dùng object pool: prefab Coin có SimpleGemsAnim chụp vị trí ở Start() rồi tự ghi đè vị trí mỗi frame,
/// tái sử dụng object sẽ làm coin nhảy về chỗ cũ.
/// </summary>
public class LocalObstacleSpawner : MonoBehaviour
{
    [SerializeField] private ObstacleSpawnPointGenerator generator;
    [Tooltip("Cùng asset SpawnProfile với generator (thư viện prefab, thông số mẫu coin).")]
    [SerializeField] private SpawnProfile profile;
    [SerializeField] private float spawnRadius = 180f; // chỉ spawn trong bán kính này quanh tâm thế giới
    [Tooltip("Số mục (entry) dựng mới tối đa mỗi frame. Khi tâm thế giới nhảy sang người ở xa sẽ có cả trăm vật cần dựng, " +
             "dồn vào một frame thì giật; chia ra vài frame thì mượt. Phần chưa dựng sẽ dựng ở các frame sau.")]
    [SerializeField] private int maxSpawnsPerFrame = 12;

    private Transform focus;
    private readonly Dictionary<int, List<GameObject>> spawned = new Dictionary<int, List<GameObject>>();
    private readonly HashSet<int> shouldExistIds = new HashSet<int>();
    private readonly List<int> toRemove = new List<int>();
    private readonly List<Vector3> coinPositions = new List<Vector3>();

    void OnEnable()
    {
        WorldFocus.EnsureExists();
        WorldFocus.OnTargetChanged += HandleFocusChanged;

        if (WorldFocus.Target != null)
            HandleFocusChanged(WorldFocus.Target);
    }

    void OnDisable()
    {
        WorldFocus.OnTargetChanged -= HandleFocusChanged;
    }

    void HandleFocusChanged(Transform target)
    {
        focus = target;
    }

    void Update()
    {
        if (focus == null || profile == null) return;
        if (generator == null || generator.Object == null || !generator.Object.IsValid) return;

        var entries = generator.Entries;
        float focusZ = focus.position.z;
        int spawnedThisFrame = 0;
        shouldExistIds.Clear();

        for (int i = 0; i < entries.Length; i++)
        {
            SpawnEntry entry = entries[i];
            if (!entry.Active) continue;

            // Mẫu dài (hàng coin) tính theo tâm của nó, nới phạm vi thêm nửa chiều dài.
            float centerZ = entry.Z + entry.Length * 0.5f;
            if (Mathf.Abs(centerZ - focusZ) > spawnRadius + entry.Length * 0.5f) continue;

            shouldExistIds.Add(entry.Id);

            if (!spawned.ContainsKey(entry.Id))
            {
                // Hết hạn mức frame này: để frame sau (entry vẫn nằm trong shouldExistIds nên không bị coi là cần dọn).
                if (spawnedThisFrame >= maxSpawnsPerFrame) continue;
                spawnedThisFrame++;

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
