using System.Collections.Generic;
using UnityEngine;

public class TracksSpawner : MonoBehaviour
{
    [SerializeField] GameObject track;
    PlayerStats player;

    [Header("Pooling Settings")]
    [SerializeField] int poolSize = 15;
    [SerializeField] int tracksBehind = 5;
    [SerializeField] int tracksAhead = 10;

    private float trackLength;
    private Stack<GameObject> pool = new Stack<GameObject>();
    private List<GameObject> activeTracks = new List<GameObject>(); // sắp xếp từ sau -> trước
    private float lastSpawnZ; // vị trí Z của player tại thời điểm gần nhất active track

    void OnEnable()
    {
        PlayerContext.OnLocalPlayerReady += HandlePlayerReady;
    }

    void OnDisable()
    {
        PlayerContext.OnLocalPlayerReady -= HandlePlayerReady;
    }

    void HandlePlayerReady(Transform localPlayer)
    {
        player = PlayerContext.Instance.LocalStats;
        SpawnInitialTracks();
    }

    void Start()
    {
        // Tự đo chiều dài track theo trục Z (player di chuyển theo trục Z)
        Renderer r = track.GetComponentInChildren<Renderer>();
        trackLength = r != null ? r.bounds.size.z : 10f;

        for (int i = 0; i < poolSize; i++)
        {
            // 1. Khởi tạo object từ prefab
            GameObject newTrack = Instantiate(track);
            // 2. Tắt object đi (để chuẩn bị sẵn trong pool, khi nào cần mới bật)
            newTrack.SetActive(false);
            // 3. Đưa object vừa tạo vào Stack
            pool.Push(newTrack);
        }
    }

    void Update()
    {
        if (player == null) return; // chưa có player thì chưa làm gì

        // Nếu player đã đi quá 1 đoạn bằng trackLength kể từ lần active track gần nhất
        if (player.transform.position.z - lastSpawnZ >= trackLength)
        {
            Recycle();
            lastSpawnZ += trackLength; // cộng dồn để tránh trôi lệch (drift)
        }
    }

    void SpawnInitialTracks()
    {
        float playerZ = player.transform.position.z;
        lastSpawnZ = playerZ;

        // 1. Bật 1 track ngay tại vị trí player
        GameObject centerTrack = GetFromPool();
        if (centerTrack != null)
        {
            centerTrack.transform.position = new Vector3(0f, 0f, playerZ);
            activeTracks.Add(centerTrack);
        }

        // 2. Bật các track phía sau: cách player trackLength/2, sau đó cách nhau trackLength
        for (int i = 0; i < tracksBehind; i++)
        {
            float zPos = playerZ - (trackLength * 0.5f) - (i * trackLength);
            GameObject t = GetFromPool();
            if (t == null) break;
            t.transform.position = new Vector3(0f, 0f, zPos);
            activeTracks.Insert(0, t); // chèn vào đầu để giữ đúng thứ tự sau -> trước
        }

        // 3. Bật các track phía trước: cách player trackLength/2, sau đó cách nhau trackLength
        for (int i = 0; i < tracksAhead; i++)
        {
            float zPos = playerZ + (trackLength * 0.5f) + (i * trackLength);
            GameObject t = GetFromPool();
            if (t == null) break;
            t.transform.position = new Vector3(0f, 0f, zPos);
            activeTracks.Add(t); // thêm vào cuối
        }
    }

    void Recycle()
    {
        if (activeTracks.Count == 0) return;

        // Lấy track sau cùng (đầu danh sách)
        GameObject rearTrack = activeTracks[0];
        activeTracks.RemoveAt(0);

        // Đặt nó lên trước track đầu tiên hiện tại (track cuối danh sách), cách trackLength
        GameObject frontTrack = activeTracks[activeTracks.Count - 1];
        float newZ = frontTrack.transform.position.z + trackLength;
        rearTrack.transform.position = new Vector3(0f, 0f, newZ);

        // Đưa vào cuối danh sách vì giờ nó là track mới nhất phía trước
        activeTracks.Add(rearTrack);
    }

    GameObject GetFromPool()
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("Pool đã hết track! Tăng poolSize nếu cần.");
            return null;
        }

        GameObject t = pool.Pop();
        t.SetActive(true);
        return t;
    }

    void ReturnToPool(GameObject t)
    {
        t.SetActive(false);
        pool.Push(t);
    }
}