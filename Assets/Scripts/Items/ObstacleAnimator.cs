using UnityEngine;

/// <summary>
/// Gắn script này vào bất kỳ GameObject (vật cản / item) nào cần hiệu ứng:
/// - Xoay (Rotation)
/// - Bay lên xuống (Bobbing / Up-Down)
/// - Bay qua lại (Side to Side)
/// - Phóng to thu nhỏ (Scale Pulse)
/// Tất cả thông số đều chỉnh được trực tiếp trên Inspector.
/// </summary>
public class ObstacleAnimator : MonoBehaviour
{
    [Header("=== XOAY (ROTATION) ===")]
    [Tooltip("Bật/tắt hiệu ứng xoay")]
    public bool enableRotation = true;

    [Tooltip("Trục xoay (VD: (0,1,0) xoay quanh trục Y)")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Tốc độ xoay (độ/giây)")]
    public float rotationSpeed = 90f;


    [Header("=== BAY LÊN XUỐNG (UP - DOWN) ===")]
    [Tooltip("Bật/tắt hiệu ứng bay lên xuống")]
    public bool enableUpDown = true;

    [Tooltip("Biên độ (khoảng cách di chuyển lên/xuống)")]
    public float upDownAmplitude = 0.5f;

    [Tooltip("Tốc độ dao động lên xuống")]
    public float upDownSpeed = 2f;


    [Header("=== BAY QUA LẠI (SIDE TO SIDE) ===")]
    [Tooltip("Bật/tắt hiệu ứng bay qua lại")]
    public bool enableSideToSide = true;

    [Tooltip("Trục di chuyển qua lại (VD: (1,0,0) là trục X)")]
    public Vector3 sideToSideAxis = Vector3.right;

    [Tooltip("Biên độ (khoảng cách di chuyển qua lại)")]
    public float sideToSideAmplitude = 0.5f;

    [Tooltip("Tốc độ dao động qua lại")]
    public float sideToSideSpeed = 1.5f;


    [Header("=== PHÓNG TO / THU NHỎ (SCALE PULSE) ===")]
    [Tooltip("Bật/tắt hiệu ứng phóng to thu nhỏ")]
    public bool enableScalePulse = true;

    [Tooltip("Tỉ lệ scale nhỏ nhất (so với scale gốc, VD 0.8 = 80%)")]
    public float minScaleMultiplier = 0.8f;

    [Tooltip("Tỉ lệ scale lớn nhất (so với scale gốc, VD 1.2 = 120%)")]
    public float maxScaleMultiplier = 1.2f;

    [Tooltip("Tốc độ phóng to thu nhỏ")]
    public float scaleSpeed = 2f;


    [Header("=== TÙY CHỌN KHÁC ===")]
    [Tooltip("Dùng thời gian ngẫu nhiên bắt đầu khác nhau giữa các object (tránh các item cùng nhịp)")]
    public bool randomizeStartOffset = true;

    // Lưu vị trí & scale gốc để tính toán dao động quanh nó
    private Vector3 startPosition;
    private Vector3 startScale;

    // Offset thời gian riêng cho mỗi object (nếu bật randomize)
    private float timeOffset;

    void Start()
    {
        startPosition = transform.position;
        startScale = transform.localScale;

        timeOffset = randomizeStartOffset ? Random.Range(0f, 100f) : 0f;
    }

    void Update()
    {
        float t = Time.time + timeOffset;

        // --- XOAY ---
        if (enableRotation)
        {
            transform.Rotate(rotationAxis.normalized * rotationSpeed * Time.deltaTime, Space.Self);
        }

        // --- TÍNH VỊ TRÍ MỚI (lên xuống + qua lại) ---
        Vector3 newPosition = startPosition;

        if (enableUpDown)
        {
            float yOffset = Mathf.Sin(t * upDownSpeed) * upDownAmplitude;
            newPosition += Vector3.up * yOffset;
        }

        if (enableSideToSide)
        {
            float sideOffset = Mathf.Sin(t * sideToSideSpeed) * sideToSideAmplitude;
            newPosition += sideToSideAxis.normalized * sideOffset;
        }

        if (enableUpDown || enableSideToSide)
        {
            transform.position = newPosition;
        }

        // --- PHÓNG TO / THU NHỎ ---
        if (enableScalePulse)
        {
            // Dao động từ 0 -> 1 -> 0 theo hình sin, rồi map sang khoảng [min, max]
            float lerpT = (Mathf.Sin(t * scaleSpeed) + 1f) / 2f;
            float scaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, lerpT);
            transform.localScale = startScale * scaleMultiplier;
        }
    }
}