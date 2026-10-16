using UnityEngine;

/// <summary>
/// Buff nam châm: trong thời gian hiệu lực, các coin trong bán kính quanh nhân vật tự bay vào người (xem CoinMagnet),
/// và một model nam châm hiện ra sau lưng nhân vật, đi theo nhân vật.
/// Tạo asset: Create > Buffs > Coin Buff (đặt trong Assets/Data/Buffs), rồi gán cho prefab CoinBuffItem.
/// Mỗi stack thêm tăng bán kính; thời gian và số stack tối đa lấy từ buffDuration / maxStack của lớp Buff.
/// Không dùng ô Buff Effect Prefab của lớp Buff (bị ẩn trên Inspector): model nam châm có ô Magnet Model riêng.
/// Không đụng tới hệ số nhân xu (CoinCalculator.multiplier) của BuffImmediately.BuffDoubleCoin cũ.
/// </summary>
[CreateAssetMenu(fileName = "CoinBuff", menuName = "Buffs/Coin Buff")]
public class CoinBuff : Buff
{
    [Header("Nam châm hút coin")]
    [Tooltip("Bán kính (mét) quanh nhân vật: coin trong phạm vi này tự bay vào người chơi.")]
    public float magnetRadius = 12f;

    [Tooltip("Mỗi stack từ stack thứ hai trở đi cộng thêm bấy nhiêu mét bán kính.")]
    public float radiusPerExtraStack = 6f;

    [Tooltip("Tốc độ bay cơ bản của coin (m/s). Coin còn được cộng thêm tốc độ chạy hiện tại của nhân vật để luôn đuổi kịp.")]
    public float flySpeed = 25f;

    [Header("Model nam châm sau lưng nhân vật")]
    [Tooltip("Prefab model nam châm (GameObject). Hiện làm con của nhân vật trong lúc buff còn hiệu lực rồi bị hủy khi hết. " +
             "Collider trên model (nếu có) sẽ bị tắt. Bỏ trống thì không hiện model.")]
    public GameObject magnetModel;

    [Tooltip("Vị trí model so với nhân vật (tọa độ cục bộ của nhân vật). Nhân vật chạy theo +Z nên Z âm là phía sau lưng.")]
    public Vector3 modelLocalPosition = new Vector3(0f, 1.2f, -0.6f);

    [Tooltip("Góc xoay model so với nhân vật (độ).")]
    public Vector3 modelLocalEuler = Vector3.zero;

    [Tooltip("Hệ số nhân thêm lên tỉ lệ gốc của prefab model.")]
    public Vector3 modelLocalScale = Vector3.one;

    public override void OnFirstApplied(PlayerStats player, BuffContext context) { }

    public override void OnStackChanged(PlayerStats player, int currentStack, BuffContext context)
    {
        CoinMagnet magnet = player.GetComponent<CoinMagnet>();

        if (currentStack <= 0)
        {
            if (magnet != null) magnet.Stop(); // Stop cũng gỡ model nam châm
            return;
        }

        if (magnet == null)
            magnet = player.gameObject.AddComponent<CoinMagnet>();

        magnet.Configure(magnetRadius + (currentStack - 1) * radiusPerExtraStack, flySpeed,
            magnetModel, modelLocalPosition, modelLocalEuler, modelLocalScale);
    }

    // Ô Buff Effect Prefab của lớp Buff không dùng cho Coin Buff (PlayerBuffManager sẽ sinh nó ngay chân nhân vật):
    private void OnValidate()
    {
        if (buffEffectPrefab == null)
            return;

        if (magnetModel == null)
            magnetModel = buffEffectPrefab;
        buffEffectPrefab = null;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
