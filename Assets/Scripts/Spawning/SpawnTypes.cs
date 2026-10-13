using Fusion;
using UnityEngine;

/// <summary>Loại vật cản: quyết định người chơi phải né bằng cách nào.</summary>
public enum ObstacleKind : byte
{
    BlockLane = 0, // chặn cả làn: phải đổi làn
    Low = 1,       // thấp: phải nhảy qua
    Overhead = 2,  // trên cao: phải trượt dưới
}

/// <summary>
/// Vật cản chiếm bao nhiêu làn. SingleLane: một làn, đặt ở tâm làn. FullWidth: một vật lớn phủ cả đường, đặt ở giữa đường;
/// chỉ hợp lệ với loại Low (nhảy qua) hoặc Overhead (trượt dưới), vì loại BlockLane phủ cả đường thì không thể qua.
/// </summary>
public enum LaneSpan : byte
{
    SingleLane = 0,
    FullWidth = 1,
}

/// <summary>Loại đồ vật một SpawnEntry mô tả.</summary>
public enum SpawnKind : byte
{
    Obstacle = 0,
    Coin = 1,     // một MẪU coin (hàng thẳng, vòng cung...), client tự bung thành từng đồng
    Buff = 2,
    Question = 3,
}

public enum CoinPattern : byte
{
    Line = 0,     // hàng thẳng dài trên một làn
    JumpArc = 1,  // vòng cung trên không theo quỹ đạo nhảy (phía trên vật cản thấp)
    AirWave = 2,  // đường cong hình sin trên không dọc một làn
    ZigZag = 3,   // đổi làn qua lại
}

/// <summary>
/// Một mục spawn đồng bộ qua mạng. Một entry = một MẪU (cả hàng coin hay vòng cung chỉ là một entry),
/// nên mảng mạng nhỏ; client tự bung thành từng đồng bằng CoinPatternBuilder (hàm xác định, mọi máy ra cùng vị trí).
/// </summary>
public struct SpawnEntry : INetworkStruct
{
    public int Id;
    public byte Kind;     // SpawnKind
    public byte Variant;  // chỉ số prefab (obstacles / buffPrefabs / questionPrefabs); với ZigZag: hướng (0 = sang phải, 1 = sang trái)
    public byte Pattern;  // CoinPattern (chỉ dùng cho Kind = Coin)
    public byte Count;    // số coin của mẫu
    public float X;       // vị trí X của làn (làn xuất phát với ZigZag)
    public float Z;       // Z của vật / đồng coin đầu tiên (JumpArc: Z tâm vòng cung)
    public float Length;  // chiều dài mẫu theo Z, do Master tính sẵn (theo tốc độ dự kiến)
    public NetworkBool Active;
}

/// <summary>
/// Trạng thái lập kế hoạch spawn, lưu [Networked] trong generator để Master mới (nếu Master cũ thoát)
/// tiếp tục liền mạch.
/// </summary>
public struct PlannerState : INetworkStruct
{
    public NetworkBool Initialized;
    public NetworkBool HasPrevRow;
    public float NextRowZ;        // Z của hàng vật cản kế tiếp
    public float LastRowZ;
    public float LastRowLength;   // chiều dài (theo Z) của hàng trước
    public int LastFreeMask;      // các làn KHÔNG có vật cản ở hàng trước (bit i = làn i)
    public int LastPassableMask;  // các làn qua được ở hàng trước (kể cả làn nhảy/trượt qua được)
    public NetworkBool LastRowAction; // hàng trước bắt buộc nhảy/trượt
    public int ChainRemaining;    // số hàng còn lại của chuỗi so le (Staggered)
    public float NextBuffZ;
    public float NextQuestionZ;
    public int NextId;
}

/// <summary>
/// Mô hình tốc độ dự kiến: v = v0 + a·t, chặn ở vMax. Dùng để đổi "thời gian" (giây) thành "khoảng cách" (mét)
/// khi lập kế hoạch: càng xa càng nhanh nên cùng một khoảng thời gian né phải ứng với khoảng cách dài hơn.
/// </summary>
public readonly struct SpeedModel
{
    private readonly float baseSpeed;
    private readonly float acceleration;
    private readonly float maxSpeed;

    public SpeedModel(float baseSpeed, float acceleration, float maxSpeed)
    {
        this.baseSpeed = baseSpeed;
        this.acceleration = acceleration;
        this.maxSpeed = maxSpeed;
    }

    /// <summary>Tốc độ dự kiến sau khi đã chạy được distance mét kể từ vạch xuất phát.</summary>
    public float At(float distance)
    {
        distance = Mathf.Max(0f, distance);
        if (acceleration <= 0f) return Mathf.Min(baseSpeed, maxSpeed);

        float tMax = (maxSpeed - baseSpeed) / acceleration;
        if (tMax <= 0f) return maxSpeed;

        float sMax = baseSpeed * tMax + 0.5f * acceleration * tMax * tMax;
        if (distance >= sMax) return maxSpeed;

        // Giải s = v0·t + a·t²/2 theo t
        float t = (-baseSpeed + Mathf.Sqrt(baseSpeed * baseSpeed + 2f * acceleration * distance)) / acceleration;
        return baseSpeed + acceleration * t;
    }
}
