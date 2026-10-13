using UnityEngine;

/// <summary>
/// Gắn tự động vào hiệu ứng do SpawnEffect.PlayFollowing tạo (không gắn tay). Mỗi frame đặt hiệu ứng theo mục tiêu
/// (+ độ lệch), hết thời gian hoặc mục tiêu mất thì trả về pool của SpawnEffect để tái sử dụng.
/// Hiệu ứng KHÔNG là con của mục tiêu nên không thừa hưởng xoay/tỉ lệ của nhân vật.
/// </summary>
public class EffectFollower : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private float endTime;
    private bool inUse;

    /// <summary>Prefab gốc (khóa của pool).</summary>
    public ParticleSystem PrefabKey { get; private set; }

    /// <summary>Hệ hạt của bản sao này.</summary>
    public ParticleSystem Particles { get; private set; }

    public bool InUse => inUse;

    public void Init(ParticleSystem prefabKey, ParticleSystem particles)
    {
        PrefabKey = prefabKey;
        Particles = particles;
    }

    public void Begin(Transform followTarget, Vector3 followOffset, float lifetime)
    {
        target = followTarget;
        offset = followOffset;
        endTime = Time.time + lifetime;
        inUse = true;
    }

    public void EndUse()
    {
        inUse = false;
        target = null;
    }

    private void LateUpdate()
    {
        if (!inUse) return;

        if (target == null || Time.time >= endTime)
        {
            SpawnEffect.ReleaseFollower(this, true);
            return;
        }

        transform.position = target.position + offset;
    }

    // Hệ hạt có "Stop Action = Disable/Destroy" tự tắt object: vẫn phải trả về pool, nếu không sẽ rò rỉ.
    private void OnDisable()
    {
        if (inUse)
            SpawnEffect.ReleaseFollower(this, false);
    }
}
