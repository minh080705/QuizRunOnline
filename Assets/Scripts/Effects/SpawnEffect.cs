// dùng để phát các hiệu ứng khi player trả lời đúng hoặc sai câu hỏi
using UnityEngine;
public class SpawnEffect : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================
    public static SpawnEffect Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // =========================================================
    // EFFECT
    // =========================================================
    [Header("Correct Effect")]
    [SerializeField] private ParticleSystem correctEffect;
    [SerializeField] private ParticleSystem correctEffect2;

    [Header("Wrong Effect")]
    [SerializeField] private ParticleSystem wrongEffect;

    [Header("Spawn Offset (trước mặt player)")]
    [SerializeField] private float forwardDistance = 13f;
    [SerializeField] private float heightOffset1 = 2f;
    [SerializeField] private float heightOffset2 = 5f;

    // =========================================================
    // PUBLIC API - gọi từ ScreenQuestion
    // =========================================================

    /// <summary>
    /// Phát hiệu ứng đúng, tại vị trí phía trước player được truyền vào.
    /// </summary>
    public void PlayCorrectEffect(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("PlayCorrectEffect: player null.");
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
            Debug.LogWarning("PlayWrongEffect: player null.");
            return;
        }

        SpawnOne(wrongEffect, GetSpawnPosition(player, heightOffset1));
    }

    // =========================================================
    // INTERNAL
    // =========================================================

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

        ParticleSystem effect = Instantiate(prefab, position, Quaternion.identity);
        effect.Play();

        float lifetime = effect.main.duration + effect.main.startLifetime.constantMax;
        Destroy(effect.gameObject, lifetime);
    }
}