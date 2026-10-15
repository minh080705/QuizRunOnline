using UnityEngine;

/// <summary>
/// Nguồn dữ liệu chung cho đường đua: Z của vạch xuất phát và vạch đích, định sẵn từ đầu trận.
/// Đặt trong RaceScene (không cần mạng: mọi máy load cùng scene nên cùng giá trị).
/// FinishLineSpawner dùng để biết spawn vạch đích ở đâu, thanh tiến trình dùng để tính tỉ lệ quãng đường.
/// </summary>
public class RaceTrackConfig : MonoBehaviour
{
    public static RaceTrackConfig Instance { get; private set; }

    [Header("Đường đua (trục Z)")]
    [Tooltip("Z của vạch xuất phát (khớp Z của các spawn point trong RaceScenePlayerSpawner).")]
    [SerializeField] private float startZ = 0f;

    [Tooltip("Chiều dài từ vạch xuất phát tới vạch đích (mét).")]
    [SerializeField] private float raceLength = 2000f;

    public float StartZ => startZ;
    public float RaceLength => raceLength;
    public float FinishZ => startZ + raceLength;

    private void Awake()
    {
        // Scene object, không DontDestroyOnLoad: mỗi lần vào RaceScene có bản mới nguyên vẹn.
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Tỉ lệ quãng đường đã đi, 0 = vạch xuất phát, 1 = vạch đích (đã clamp).</summary>
    public float Progress01(float z)
    {
        return Mathf.InverseLerp(startZ, FinishZ, z);
    }

    private void OnValidate()
    {
        if (raceLength < 1f) raceLength = 1f;
    }
}
