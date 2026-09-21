using System.Collections.Generic;
using UnityEngine;

public class LocalObstacleSpawner : MonoBehaviour
{
    [SerializeField] private ObstacleSpawnPointGenerator generator;
    [SerializeField] private GameObject[] obstaclePrefabs; // kéo thả trong Inspector, index khớp ObstacleIndex
    [SerializeField] private float spawnRadius = 60f;       // chỉ spawn trong bán kính này quanh player

    private Transform localPlayer;
    private Dictionary<int, GameObject> spawnedObstacles = new Dictionary<int, GameObject>();

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
        if (localPlayer == null) return;

        var points = generator.SpawnPoints;
        var shouldExistIds = new HashSet<int>();

        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            if (!point.Active) continue;

            float distance = Mathf.Abs(point.Position.z - localPlayer.position.z);
            if (distance > spawnRadius) continue; // ngoài phạm vi hiển thị của local player

            shouldExistIds.Add(point.Id);

            if (!spawnedObstacles.ContainsKey(point.Id))
            {
                GameObject prefab = obstaclePrefabs[point.ObstacleIndex];
                Vector3 spawnPos = new Vector3(point.Position.x, prefab.transform.position.y, point.Position.z);
                GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);
                spawnedObstacles[point.Id] = instance;
            }
        }

        // Dọn obstacle không còn nên tồn tại (đã bị Master vô hiệu hoá, hoặc đã ra khỏi bán kính)
        List<int> toRemove = new List<int>();
        foreach (var kvp in spawnedObstacles)
        {
            if (!shouldExistIds.Contains(kvp.Key)) toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove)
        {
            if (spawnedObstacles[id] != null) Destroy(spawnedObstacles[id]);
            spawnedObstacles.Remove(id);
        }
    }
}