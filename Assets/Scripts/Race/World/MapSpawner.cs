using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dựng map theo từng khối dài blockLength quanh "tâm thế giới" (WorldFocus). Giữ khối chứa điểm cách lookAhead mét phía trước
/// và khối liền trước. Khối bị gỡ được tắt và giữ lại trong pool theo từng prefab, tránh Instantiate/Destroy hàng nghìn object mỗi khối.
/// </summary>
public class MapSpawner : MonoBehaviour
{
    [Header("Map Prefabs")]
    public GameObject[] maps = new GameObject[5];

    [Header("Vị trí đặt map")]
    [Tooltip("X chung của mọi map. Các prefab map được dựng sao cho đường chạy nằm lệch khoảng +3 m so với gốc prefab, nên gốc map " +
             "phải đặt ở khoảng -2.5 (đã đo: -2.5 khớp đường ray, 0 làm map lệch phải). Chỉnh số này nếu map vẫn lệch ngang. " +
             "Cố định, không phụ thuộc điểm spawn hay làn của người chơi.")]
    [SerializeField] private float mapOriginX = -2.5f;

    [Header("Khối map")]
    [SerializeField] private float blockLength = 500f;
    [Tooltip("Khối kế tiếp được dựng khi tâm thế giới còn cách đầu khối này chừng này mét.")]
    [SerializeField] private float lookAhead = 250f;
    [Tooltip("Số bản sao tắt được giữ lại tối đa cho mỗi loại map để dùng lại. Vượt quá thì Destroy.")]
    [SerializeField] private int maxPooledPerMap = 2;

    private readonly Dictionary<int, GameObject> blocks = new Dictionary<int, GameObject>();       // khối -> bản sao đang bật
    private readonly Dictionary<int, int> blockMapIndex = new Dictionary<int, int>();              // khối -> map đã chọn (nhớ để quay lại thấy đúng)
    private readonly Dictionary<int, Stack<GameObject>> pool = new Dictionary<int, Stack<GameObject>>(); // map -> bản sao rảnh
    private readonly List<int> removeBuffer = new List<int>();

    private Transform focus;
    private float originZ;
    private bool originKnown;
    private int lastBlockKey = int.MinValue;

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
        if (focus == null) return;

        if (!originKnown)
        {
            originZ = RaceTrackConfig.Instance != null ? RaceTrackConfig.Instance.StartZ : focus.position.z;
            originKnown = true;
        }

        lastBlockKey = int.MinValue; // buộc Update tính lại tập khối
    }

    void Update()
    {
        if (focus == null || !originKnown) return; // chưa có tâm thì chưa làm gì
        if (maps == null || maps.Length == 0) return;

        // Khối chứa điểm cách tâm lookAhead mét phía trước; chỉ làm việc khi nó đổi.
        int key = Mathf.FloorToInt((focus.position.z + lookAhead - originZ) / blockLength);
        if (key == lastBlockKey) return;
        lastBlockKey = key;

        int high = Mathf.Max(key, 0);
        int low = Mathf.Max(key - 1, 0);

        removeBuffer.Clear();
        foreach (int block in blocks.Keys)
        {
            if (block < low || block > high) removeBuffer.Add(block);
        }
        foreach (int block in removeBuffer)
            ReleaseBlock(block);

        for (int block = low; block <= high; block++)
        {
            if (!blocks.ContainsKey(block))
                PlaceBlock(block);
        }
    }

    void PlaceBlock(int block)
    {
        if (!blockMapIndex.TryGetValue(block, out int mapIndex))
        {
            mapIndex = Random.Range(0, maps.Length);
            blockMapIndex[block] = mapIndex;
        }

        GameObject prefab = maps[mapIndex];
        if (prefab == null) return;

        // Tâm khối: prefab map có gốc ở giữa, trải dài blockLength theo Z.
        Vector3 position = new Vector3(mapOriginX, 0f, originZ + blockLength * (block + 0.5f));

        GameObject instance = null;
        if (pool.TryGetValue(mapIndex, out Stack<GameObject> idle))
        {
            while (idle.Count > 0 && instance == null)
                instance = idle.Pop(); // bản sao đã bị hủy theo scene thì bỏ qua
        }

        if (instance != null)
        {
            instance.transform.SetPositionAndRotation(position, prefab.transform.rotation);
            instance.SetActive(true);
        }
        else
        {
            instance = Instantiate(prefab, position, prefab.transform.rotation);
        }

        blocks[block] = instance;
    }

    void ReleaseBlock(int block)
    {
        if (!blocks.TryGetValue(block, out GameObject instance)) return;
        blocks.Remove(block);
        if (instance == null) return;

        int mapIndex = blockMapIndex[block];
        if (!pool.TryGetValue(mapIndex, out Stack<GameObject> idle))
        {
            idle = new Stack<GameObject>();
            pool[mapIndex] = idle;
        }

        if (idle.Count >= maxPooledPerMap)
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        idle.Push(instance);
    }
}
