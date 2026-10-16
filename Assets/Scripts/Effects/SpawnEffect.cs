using System.Collections.Generic;
using UnityEngine;
/// <summary>Phát hiệu ứng khi trả lời đúng/sai và khi nhặt item hoặc va chạm (PlayAt: nổ một lần tại chỗ, PlayFollowing: đi theo nhân vật).</summary>
public class SpawnEffect : MonoBehaviour
{
    public static SpawnEffect Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [Header("Correct Effect")]
    [SerializeField] private ParticleSystem correctEffect;
    [SerializeField] private ParticleSystem correctEffect2;

    [Header("Wrong Effect")]
    [SerializeField] private ParticleSystem wrongEffect;

    [Header("Spawn Offset (trước mặt player)")]
    [SerializeField] private float forwardDistance = 13f;
    [SerializeField] private float heightOffset1 = 2f;
    [SerializeField] private float heightOffset2 = 5f;

    /// <summary>
    /// Phát hiệu ứng đúng, tại vị trí phía trước player được truyền vào.
    /// </summary>
    public void PlayCorrectEffect(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        SpawnOne(correctEffect, GetSpawnPosition(player, heightOffset1));
        SpawnOne(correctEffect2, GetSpawnPosition(player, heightOffset2));
    }

    /// <summary>
    /// Phát hiệu ứng sai, tại vị trí phía trước player được truyền vào.
    /// </summary>
    public void PlayWrongEffect(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        SpawnOne(wrongEffect, GetSpawnPosition(player, heightOffset1));
    }

    private Vector3 GetSpawnPosition(GameObject player, float heightOffset)
    {
        return player.transform.position
             + player.transform.forward * forwardDistance
             + player.transform.up * heightOffset;
    }

    private void SpawnOne(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogWarning("SpawnEffect: chưa gán prefab particle.");
            return;
        }

        PlayAt(prefab, position);
    }

    /// <summary>
    /// Phát một hiệu ứng hạt MỘT LẦN tại vị trí cho trước rồi tự hủy sau khi chạy xong. Hàm static: không cần
    /// SpawnEffect.Instance trong scene, và hiệu ứng KHÔNG là con của ai (nên vẫn chạy tiếp khi vật vừa nhặt đã bị Destroy).
    /// Dùng cho hiệu ứng nhặt coin/item.
    /// </summary>
    public static void PlayAt(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        ParticleSystem effect = Instantiate(prefab, position, Quaternion.identity);
        effect.Play();

        float lifetime = effect.main.duration + effect.main.startLifetime.constantMax;
        Destroy(effect.gameObject, lifetime);
    }

    // Pool theo prefab: coin dày (hàng chục đồng mỗi giây) nên tái sử dụng bản sao thay vì Instantiate/Destroy liên tục.
    // Là static nên còn nguyên khi đổi scene; bản sao bị hủy cùng scene sẽ thành null và được bỏ qua khi lấy ra.
    private static readonly Dictionary<ParticleSystem, Stack<EffectFollower>> followerPool =
        new Dictionary<ParticleSystem, Stack<EffectFollower>>();

    /// <summary>
    /// Phát hiệu ứng hạt MỘT LẦN và cho nó ĐI THEO target (vị trí target + offset, offset tính theo trục thế giới)
    /// cho tới khi chạy xong. Hàm static, không cần SpawnEffect.Instance. Hiệu ứng không là con của target.
    /// Trong prefab hiệu ứng nên để Stop Action = None và tắt Looping. Simulation Space = Local: hạt dính vào nhân vật
    /// (hào quang); World: hạt bị bỏ lại tạo vệt đuôi.
    /// </summary>
    public static void PlayFollowing(ParticleSystem prefab, Transform target, Vector3 offset)
    {
        if (prefab == null || target == null)
            return;

        EffectFollower follower = RentFollower(prefab);
        follower.transform.position = target.position + offset;
        follower.gameObject.SetActive(true);

        ParticleSystem particles = follower.Particles;
        particles.Clear(true);
        particles.Play(true);

        follower.Begin(target, offset, particles.main.duration + particles.main.startLifetime.constantMax);
    }

    private static EffectFollower RentFollower(ParticleSystem prefab)
    {
        if (followerPool.TryGetValue(prefab, out Stack<EffectFollower> stack))
        {
            while (stack.Count > 0)
            {
                EffectFollower pooled = stack.Pop();
                if (pooled != null) return pooled; // bản sao đã bị hủy theo scene thì bỏ qua
            }
        }

        ParticleSystem instance = Instantiate(prefab);
        EffectFollower follower = instance.gameObject.AddComponent<EffectFollower>();
        follower.Init(prefab, instance);
        return follower;
    }

    /// <summary>Trả bản sao về pool (do EffectFollower gọi). deactivate = false khi object đã tự tắt.</summary>
    public static void ReleaseFollower(EffectFollower follower, bool deactivate)
    {
        if (follower == null || !follower.InUse)
            return;

        follower.EndUse();
        if (deactivate)
            follower.gameObject.SetActive(false);

        if (!followerPool.TryGetValue(follower.PrefabKey, out Stack<EffectFollower> stack))
        {
            stack = new Stack<EffectFollower>();
            followerPool[follower.PrefabKey] = stack;
        }
        stack.Push(follower);
    }
}