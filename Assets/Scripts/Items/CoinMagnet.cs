using UnityEngine;

/// <summary>
/// Nam châm hút coin: định kỳ quét các coin trong bán kính quanh nhân vật và cho chúng bay vào người, đồng thời giữ
/// model nam châm (nếu có) làm con của nhân vật để nó đi theo.
/// Không gắn tay: CoinBuff tự AddComponent lên nhân vật khi dùng Coin Buff và gọi Stop khi hết hiệu lực.
/// Chỉ tồn tại ở máy của người dùng buff (Buff.ApplyBuff chỉ chạy với nhân vật mình có quyền), và coin là object cục bộ
/// từng máy, nên chỉ máy đó thấy coin bay và model nam châm; người chơi khác không bị ảnh hưởng.
/// </summary>
public class CoinMagnet : MonoBehaviour
{
    [Tooltip("Số giây giữa hai lần quét tìm coin trong phạm vi.")]
    [SerializeField] private float scanInterval = 0.1f;

    private float radius;
    private float flySpeed;
    private float nextScanTime;
    private GameObject modelInstance;

    /// <summary>
    /// Bật nam châm với bán kính và tốc độ bay cơ bản cho trước; hiện model nam châm sau lưng nếu có.
    /// Gọi lại khi số stack đổi chỉ cập nhật thông số, model đã có thì giữ nguyên.
    /// </summary>
    public void Configure(float newRadius, float newFlySpeed, GameObject modelPrefab,
        Vector3 modelLocalPosition, Vector3 modelLocalEuler, Vector3 modelLocalScale)
    {
        radius = newRadius;
        flySpeed = newFlySpeed;
        enabled = true;

        if (modelInstance == null && modelPrefab != null)
            modelInstance = CreateModel(modelPrefab, modelLocalPosition, modelLocalEuler, modelLocalScale);
    }

    /// <summary>Tắt nam châm và gỡ model (coin đang bay vẫn tiếp tục bay tới nhân vật).</summary>
    public void Stop()
    {
        enabled = false; // OnDisable gỡ model
    }

    private GameObject CreateModel(GameObject prefab, Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
    {
        GameObject instance = Instantiate(prefab, transform);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        instance.transform.localScale = Vector3.Scale(prefab.transform.localScale, localScale);

        // Model chỉ để trang trí: collider trên đó không được va vào vật cản hay coin.
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        return instance;
    }

    private void OnEnable()
    {
        nextScanTime = 0f; // quét ngay khi bật
    }

    private void OnDisable()
    {
        if (modelInstance != null)
        {
            Destroy(modelInstance);
            modelInstance = null;
        }
    }

    private void Update()
    {
        if (Time.time < nextScanTime)
            return;
        nextScanTime = Time.time + scanInterval;

        float sqrRadius = radius * radius;
        Vector3 center = transform.position;
        var coins = CoinItem.Active;

        for (int i = 0; i < coins.Count; i++)
        {
            CoinItem coin = coins[i];
            if (coin.IsAttracted)
                continue;

            if ((coin.transform.position - center).sqrMagnitude <= sqrRadius)
                coin.AttractTo(transform, flySpeed);
        }
    }
}
