using Fusion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bộ sinh dữ liệu spawn (chỉ Master chạy; các máy khác chỉ đọc). Dùng SpawnPlanner để lập kế hoạch từng "hàng"
/// vật cản kèm coin/item/câu hỏi theo độ khó tăng dần, ghi vào NetworkArray&lt;SpawnEntry&gt;.
/// LocalObstacleSpawner trên mỗi client đọc mảng này và dựng object cục bộ (xem LocalObstacleSpawner).
/// </summary>
public class ObstacleSpawnPointGenerator : NetworkBehaviour
{
    [Header("Cấu hình spawn (độ khó, thư viện prefab, mẫu coin)")]
    [SerializeField] private SpawnProfile profile;

    [Header("Settings")]
    [SerializeField] private float spawnAheadDistance = 100f; // sinh trước leader bao xa
    [SerializeField] private float despawnBufferDistance = 20f; // khoảng đệm phía sau trước khi thật sự xóa
    [Tooltip("Không sinh vật cản trong đoạn này ngay trước vạch đích (và không sinh sau vạch). Cần RaceTrackConfig trong scene.")]
    [SerializeField] private float clearZoneBeforeFinish = 30f;
    [Tooltip("Số hàng tối đa lập kế hoạch trong một tick (tránh dồn việc khi mới vào trận).")]
    [SerializeField] private int maxRowsPerTick = 6;

    private const int Capacity = 96;

    [Networked, Capacity(96)] public NetworkArray<SpawnEntry> Entries => default;
    [Networked] private PlannerState State { get; set; }

    private readonly List<SpawnEntry> plan = new List<SpawnEntry>(16);
    private bool warnedMissingProfile;

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return; // chỉ Master tính toán, client khác chỉ đọc

        if (profile == null)
        {
            if (!warnedMissingProfile)
            {
                warnedMissingProfile = true;
                Debug.LogError("[ObstacleSpawnPointGenerator] Chưa gán SpawnProfile.");
            }
            return;
        }

        if (RacePlayersRegistry.Instance == null) return;
        var leader = RacePlayersRegistry.Instance.GetLeadingPlayer();
        var trailer = RacePlayersRegistry.Instance.GetTrailingPlayer();
        if (leader == null || trailer == null) return;

        float trackStartZ = RaceTrackConfig.Instance != null ? RaceTrackConfig.Instance.StartZ : 0f;
        float trackLength = RaceTrackConfig.Instance != null ? RaceTrackConfig.Instance.RaceLength : 2000f;

        PlannerState st = State;
        if (!st.Initialized)
        {
            st = SpawnPlanner.InitState(profile, trackStartZ);
            State = st;
        }

        var speed = new SpeedModel(leader.BaseSpeed, leader.AccelerationRate, leader.MaxSpeed);

        // Dừng sinh vật cản khi hàng kế tiếp rơi vào vùng trống trước vạch đích (hoặc sau vạch).
        float limitZ = RaceTrackConfig.Instance != null
            ? RaceTrackConfig.Instance.FinishZ - clearZoneBeforeFinish
            : float.MaxValue;

        float aheadZ = leader.transform.position.z + spawnAheadDistance;

        for (int row = 0; row < maxRowsPerTick; row++)
        {
            if (st.NextRowZ >= aheadZ || st.NextRowZ >= limitZ) break;

            PlannerState next = st;
            plan.Clear();
            SpawnPlanner.PlanNextRow(profile, ref next, speed, trackStartZ, trackLength, plan);

            // Không đủ chỗ cho cả hàng: dừng và thử lại tick sau. KHÔNG tăng NextRowZ, nếu không sẽ để lại
            // một đoạn đường trống phía trước (lỗi của bản cũ khi hết slot).
            if (CountFreeSlots() < plan.Count) break;

            WriteEntries(plan);
            st = next;
            State = st;
        }

        // Vô hiệu hóa mục đã nằm hẳn phía sau người chạy chậm nhất.
        float trailerZ = trailer.transform.position.z;
        for (int i = 0; i < Capacity; i++)
        {
            SpawnEntry e = Entries[i];
            if (e.Active && e.Z + e.Length < trailerZ - despawnBufferDistance)
            {
                e.Active = false;
                Entries.Set(i, e);
            }
        }
    }

    private int CountFreeSlots()
    {
        int free = 0;
        for (int i = 0; i < Capacity; i++)
            if (!Entries[i].Active) free++;
        return free;
    }

    private void WriteEntries(List<SpawnEntry> entries)
    {
        int slot = 0;
        foreach (SpawnEntry entry in entries)
        {
            while (slot < Capacity && Entries[slot].Active) slot++;
            if (slot >= Capacity) return; // đã kiểm tra CountFreeSlots nên không xảy ra

            Entries.Set(slot, entry);
            slot++;
        }
    }

    // ----------------------------------------------------------------------
    // KIỂM THỬ: chuột phải component trong Inspector > "Simulate spawn"
    // ----------------------------------------------------------------------

    [ContextMenu("Simulate spawn (kiểm tra bất biến, xem Console)")]
    private void SimulateInEditor()
    {
        if (profile == null)
        {
            Debug.LogError("[Simulate] Chưa gán SpawnProfile.");
            return;
        }

        var speed = new SpeedModel(profile.fallbackBaseSpeed, profile.fallbackAcceleration, profile.fallbackMaxSpeed);
        RaceTrackConfig track = FindObjectOfType<RaceTrackConfig>();
        float startZ = track != null ? track.StartZ : 0f;
        float length = track != null ? track.RaceLength : 2000f;

        int totalViolations = 0;
        for (int seed = 1; seed <= 20; seed++)
        {
            string report = SpawnPlanner.Simulate(profile, seed, 80, speed, startZ, length);
            if (!report.EndsWith("Vi phạm: 0.")) totalViolations++;
            Debug.Log(report);
        }

        Debug.Log(totalViolations == 0
            ? "[Simulate] Xong 20 seed: KHÔNG có vi phạm."
            : $"[Simulate] Có {totalViolations}/20 seed vi phạm, xem các dòng [VI PHẠM] ở trên.");
    }
}
