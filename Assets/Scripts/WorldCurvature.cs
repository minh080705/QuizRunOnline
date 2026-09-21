using UnityEngine;

/// <summary>
/// Điều khiển độ cong "world curve" cho toàn bộ scene (endless runner).
/// Gắn script này vào 1 GameObject duy nhất trong scene (ví dụ: GameManager).
/// Mọi shader dùng biến global "_WorldCurveStrength" và "_WorldCurveCamPos"
/// sẽ tự động đồng bộ theo giá trị ở đây.
/// </summary>
[ExecuteAlways]
public class WorldCurvature : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Camera dùng làm gốc tính khoảng cách cong. Để trống sẽ tự lấy Camera.main")]
    public Transform referenceCamera;

    [Header("Độ cong")]
    [Tooltip("Càng lớn, đường càng cong gấp xuống nhanh. Giá trị tham khảo: 0.0005 - 0.003")]
    [Range(0f, 0.01f)]
    public float curveStrength = 0.0012f;

    [Tooltip("Trục dùng làm 'khoảng cách xa' để tính độ cong. " +
             "Thường là true nếu game chạy dọc theo trục Z thế giới.")]
    public bool useWorldZAsDistance = true;

    // ID biến shader global, cache lại cho hiệu năng
    static readonly int ID_CurveStrength = Shader.PropertyToID("_WorldCurveStrength");
    static readonly int ID_CamPos = Shader.PropertyToID("_WorldCurveCamPos");
    static readonly int ID_UseZ = Shader.PropertyToID("_WorldCurveUseZ");

    void OnEnable()
    {
        if (referenceCamera == null && Camera.main != null)
            referenceCamera = Camera.main.transform;

        Push();
    }

    void Update()
    {
        // Update mỗi frame vì camera di chuyển liên tục trong endless runner
        Push();
    }

    void Push()
    {
        if (referenceCamera == null) return;

        Shader.SetGlobalFloat(ID_CurveStrength, curveStrength);
        Shader.SetGlobalVector(ID_CamPos, referenceCamera.position);
        Shader.SetGlobalFloat(ID_UseZ, useWorldZAsDistance ? 1f : 0f);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Push();
    }
#endif
}