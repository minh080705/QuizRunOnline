using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đường ray (section track) dựng bằng pool quanh "tâm thế giới" (WorldFocus): nhân vật local, hoặc người đang được camera
/// theo dõi khi mình hết mạng / đã về đích. Khi tâm đổi sang người khác thì đặt lại cả dải track quanh Z mới (chỉ dời vị trí
/// các object trong pool, rất rẻ); khi tâm đang chạy tới thì tái chế track phía sau lên phía trước như trước.
/// </summary>
public class TracksSpawner : MonoBehaviour
{
    [SerializeField] GameObject track;
    Transform focus;

    [Header("Pooling Settings")]
    [SerializeField] int poolSize = 15;
    [SerializeField] int tracksBehind = 5;
    [SerializeField] int tracksAhead = 10;

    private float trackLength;
    private bool poolReady;
    private Stack<GameObject> pool = new Stack<GameObject>();
    private List<GameObject> activeTracks = new List<GameObject>(); // sắp xếp từ sau -> trước
    private float lastSpawnZ; // vị trí Z của tâm tại thời điểm gần nhất active track

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

        // Pool chưa tạo xong (Start chưa chạy): Start sẽ tự bố trí khi xong.
        if (focus != null && poolReady)
            RelayoutAroundFocus();
    }

    void Start()
    {
        // Tự đo chiều dài track theo trục Z (player di chuyển theo trục Z)
        Renderer r = track.GetComponentInChildren<Renderer>();
        trackLength = r != null ? r.bounds.size.z : 10f;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject newTrack = Instantiate(track);
            newTrack.SetActive(false);
            pool.Push(newTrack);
        }

        poolReady = true;

        if (focus != null)
            RelayoutAroundFocus();
    }

    void Update()
    {
        if (focus == null || !poolReady) return; // chưa có tâm thì chưa làm gì

        if (focus.position.z - lastSpawnZ >= trackLength)
        {
            Recycle();
            lastSpawnZ += trackLength; // cộng dồn để tránh trôi lệch (drift)
        }
    }

    // Trả mọi track đang bật về pool rồi bố trí lại quanh Z của tâm hiện tại.
    void RelayoutAroundFocus()
    {
        foreach (GameObject t in activeTracks)
        {
            if (t != null) ReturnToPool(t);
        }
        activeTracks.Clear();

        SpawnInitialTracks();
    }

    void SpawnInitialTracks()
    {
        float focusZ = focus.position.z;
        lastSpawnZ = focusZ;

        GameObject centerTrack = GetFromPool();
        if (centerTrack != null)
        {
            centerTrack.transform.position = new Vector3(0f, 0f, focusZ);
            activeTracks.Add(centerTrack);
        }

        for (int i = 0; i < tracksBehind; i++)
        {
            float zPos = focusZ - (trackLength * 0.5f) - (i * trackLength);
            GameObject t = GetFromPool();
            if (t == null) break;
            t.transform.position = new Vector3(0f, 0f, zPos);
            activeTracks.Insert(0, t); // chèn vào đầu để giữ đúng thứ tự sau -> trước
        }

        for (int i = 0; i < tracksAhead; i++)
        {
            float zPos = focusZ + (trackLength * 0.5f) + (i * trackLength);
            GameObject t = GetFromPool();
            if (t == null) break;
            t.transform.position = new Vector3(0f, 0f, zPos);
            activeTracks.Add(t); // thêm vào cuối
        }
    }

    void Recycle()
    {
        if (activeTracks.Count == 0) return;

        GameObject rearTrack = activeTracks[0];
        activeTracks.RemoveAt(0);

        GameObject frontTrack = activeTracks[activeTracks.Count - 1];
        float newZ = frontTrack.transform.position.z + trackLength;
        rearTrack.transform.position = new Vector3(0f, 0f, newZ);

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
