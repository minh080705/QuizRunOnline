using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObstacleEntry
{
    public GameObject prefab;
    public ObstacleKind kind;
    [Tooltip("Chiều dài (theo trục Z, mét) của vật cản, dùng để chừa khoảng cách giữa các hàng và không đặt coin đè lên.")]
    public float length = 2f;
}

/// <summary>
/// Toàn bộ số liệu chỉnh tay cho việc spawn: thư viện prefab, độ khó theo tiến độ, mẫu coin, nhịp item/câu hỏi.
/// Tạo asset: Create > Spawning > Spawn Profile, rồi gán cho ObstacleSpawnPointGenerator và LocalObstacleSpawner.
/// Tiến độ p = (Z - StartZ) / RaceLength (0 ở vạch xuất phát, 1 ở vạch đích); các AnimationCurve có x = p.
/// </summary>
[CreateAssetMenu(menuName = "Spawning/Spawn Profile", fileName = "SpawnProfile")]
public class SpawnProfile : ScriptableObject
{
    [Header("Thư viện prefab")]
    [Tooltip("Mỗi vật cản kèm loại: BlockLane (đổi làn), Low (nhảy), Overhead (trượt). Mẫu cần loại nào chưa có prefab sẽ tự bị bỏ qua.")]
    public ObstacleEntry[] obstacles;
    public GameObject coinPrefab;
    public GameObject[] buffPrefabs;
    public GameObject[] questionPrefabs;

    [Header("Làn đường (X)")]
    public float[] laneX = { -2.5f, 0f, 2.5f };

    [Header("Độ khó theo tiến độ (x = 0..1)")]
    [Tooltip("Thời gian (giây) giữa hai hàng vật cản. Càng nhỏ càng khó. Quy đổi ra mét bằng tốc độ dự kiến.")]
    public AnimationCurve rowGapSeconds = new AnimationCurve(new Keyframe(0f, 2.2f), new Keyframe(1f, 0.9f));
    [Tooltip("Xác suất một hàng chặn 2 làn thay vì 1 (chỉ khi đã mở khóa).")]
    public AnimationCurve doubleLaneChance = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 0.5f));
    [Tooltip("Hàng vật cản đầu tiên cách vạch xuất phát chừng này mét.")]
    public float firstRowOffset = 40f;
    [Tooltip("Khoảng cách tối thiểu (mét) giữa hai hàng, dù tốc độ còn thấp.")]
    public float minRowGap = 8f;

    [Header("Mở khóa mẫu vật cản (tiến độ 0..1)")]
    public float doubleUnlock = 0.1f;
    public float staggeredUnlock = 0.25f;
    public float lowWallUnlock = 0.2f;
    public float overheadUnlock = 0.35f;
    public float mixedUnlock = 0.5f;

    [Header("Trọng số mẫu đặc biệt (so với mẫu 1 làn/2 làn)")]
    public float staggeredWeight = 0.6f;
    public float lowWallWeight = 0.6f;
    public float overheadWeight = 0.6f;
    public float mixedWeight = 0.5f;

    [Header("Coin")]
    [Tooltip("Xác suất có một mẫu coin trong khoảng trống giữa hai hàng.")]
    public AnimationCurve coinChance = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(1f, 0.5f));
    public float coinSpacing = 1.6f;
    public int minLineCoins = 6;
    public int maxLineCoins = 14;
    public int waveMinCoins = 8;
    public int waveMaxCoins = 14;
    [Tooltip("Độ cao đỉnh sóng coin trên không (nhỏ hơn jumpHeight để nhảy là với tới).")]
    public float waveAmplitude = 1.6f;
    [Tooltip("Số nhịp sóng trong một đường coin trên không.")]
    public float waveCycles = 2f;
    public int zigzagMinCoins = 5;
    public int zigzagMaxCoins = 8;
    public float lineWeight = 1f;
    public float waveWeight = 0.7f;
    public float zigzagWeight = 0.5f;
    [Tooltip("Chừa mỗi đầu khoảng trống chừng này mét trước khi đặt coin/item (tránh sát vật cản).")]
    public float coinPadding = 3f;
    [Tooltip("Khoảng trống ngắn hơn mức này thì không đặt coin/item.")]
    public float minFillLength = 8f;
    [Tooltip("Chiều dài tối đa (mét) của vòng cung coin trên vật cản thấp.")]
    public float maxArcLength = 40f;

    [Header("Item và câu hỏi")]
    [Tooltip("Trung bình bao nhiêu mét thì có một buff.")]
    public float buffEveryMeters = 250f;
    [Tooltip("Trung bình bao nhiêu mét thì có một câu hỏi (theo tiến độ).")]
    public AnimationCurve questionEveryMeters = new AnimationCurve(new Keyframe(0f, 180f), new Keyframe(1f, 110f));

    [Header("An toàn / công bằng (khớp với nhân vật)")]
    [Tooltip("Thời gian đổi 1 làn = bề rộng làn / sideSpeed (2.5 / 10).")]
    public float laneChangeTime = 0.25f;
    [Tooltip("Thời gian phản ứng cộng thêm (giây) giữa hai hàng cần đổi làn.")]
    public float reactionMargin = 0.35f;
    [Tooltip("Khớp PlayerJump.jumpHeight / jumpDuration.")]
    public float jumpHeight = 2f;
    public float jumpDuration = 0.6f;
    [Tooltip("Khớp PlayerSlide.slideDuration.")]
    public float slideDuration = 1f;

    [Header("Chỉ dùng cho kiểm thử (ContextMenu Simulate) khi chưa có nhân vật")]
    public float fallbackBaseSpeed = 10f;
    public float fallbackAcceleration = 0.3f;
    public float fallbackMaxSpeed = 300f;

    /// <summary>Thời gian (giây) cần để hồi lại sau một hàng bắt buộc nhảy/trượt trước khi tới hàng kế.</summary>
    public float ActionLockTime => Mathf.Max(jumpDuration, slideDuration) + reactionMargin;

    public int LaneCount => laneX != null ? laneX.Length : 0;

    public bool HasKind(ObstacleKind kind)
    {
        if (obstacles == null) return false;
        for (int i = 0; i < obstacles.Length; i++)
            if (obstacles[i] != null && obstacles[i].prefab != null && obstacles[i].kind == kind) return true;
        return false;
    }

    /// <summary>Chọn ngẫu nhiên một prefab thuộc loại kind; trả -1 nếu chưa có prefab nào.</summary>
    public int PickObstacle(ObstacleKind kind)
    {
        if (obstacles == null) return -1;

        var candidates = new List<int>(4);
        for (int i = 0; i < obstacles.Length; i++)
            if (obstacles[i] != null && obstacles[i].prefab != null && obstacles[i].kind == kind) candidates.Add(i);

        return candidates.Count == 0 ? -1 : candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    public float ObstacleLength(int variant)
    {
        return obstacles != null && variant >= 0 && variant < obstacles.Length && obstacles[variant] != null
            ? Mathf.Max(0.5f, obstacles[variant].length)
            : 2f;
    }
}
