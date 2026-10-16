using System.Collections;
using UnityEngine;

// dùng để gọi buff ngay lập tức trên người player
public class BuffImmediately : MonoBehaviour
{
    public static BuffImmediately Instance { get; private set; }

    [SerializeField] private PlayerStats player;
    [SerializeField] private PlayerCollisionHandler playerCollisionHandler;
    
    [Header("Speed Buff Settings")]
    [SerializeField] private SpeedBuff speedBuff;

    [Header("Slow Debuff Settings")]
    [SerializeField] public GameObject SlowDebuffEffectPrefab;
    [SerializeField] public float SlowDebuffDuration = 5f;
    [SerializeField] public float SlowDebuffMultiplier = 0.7f;
    [SerializeField] private int maxSlowStack = 3;

    [Header("Shield Buff Settings")]
    [SerializeField] public GameObject ShieldBuffEffectPrefab;
    [SerializeField] public float ShieldBuffDuration = 15f;
    [SerializeField] private int maxShieldStack = 5;

    [Header("Coin Buff Settings")]
    [SerializeField] public GameObject CoinBuffEffectPrefab;
    [SerializeField] public float CoinBuffDuration = 10f;
    [SerializeField] public int coinBuffMultiplier = 2;
    [SerializeField] private int maxCoinStack = 3;

    // effect hiện tại đang chạy trên player (dùng chung 1 slot hiển thị)
    private GameObject currentEffect;

    private int currentSlowStack = 0;
    private float slowBaseSideSpeed = -1f;
    private float slowBaseMoveSpeed = -1f;

    private int currentShieldStack = 0;

    private int currentCoinStack = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayEffect(GameObject effectPrefab, float duration)
    {
        if (currentEffect != null)
        {
            Destroy(currentEffect);
        }

        if (effectPrefab != null)
        {
            currentEffect = Instantiate(
                effectPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );
        }

        StartCoroutine(RemoveEffect(duration));
    }

    private IEnumerator RemoveEffect(float time)
    {
        yield return new WaitForSeconds(time);
        if (currentEffect != null)
        {
            Destroy(currentEffect);
        }
    }

    public void SlowDebuff()
    {
        if (currentSlowStack == 0)
        {
            slowBaseSideSpeed = player.sideSpeed;
            slowBaseMoveSpeed = player.moveSpeed;
        }

        if (currentSlowStack >= maxSlowStack)
        {
            return;
        }

        currentSlowStack++;
        ApplySlowByStack();
        PlayEffect(SlowDebuffEffectPrefab, SlowDebuffDuration);
        StartCoroutine(SlowStackTimeout());
    }

    private IEnumerator SlowStackTimeout()
    {
        yield return new WaitForSeconds(SlowDebuffDuration);
        if (currentSlowStack > 0)
        {
            currentSlowStack--;
            ApplySlowByStack();
        }
    }

    private void ApplySlowByStack()
    {
        if (currentSlowStack == 0)
        {
            player.sideSpeed = slowBaseSideSpeed;
            player.moveSpeed = slowBaseMoveSpeed;
            return;
        }

        float multiplier = Mathf.Pow(SlowDebuffMultiplier, currentSlowStack);
        player.sideSpeed = slowBaseSideSpeed * multiplier;
        player.moveSpeed = slowBaseMoveSpeed * multiplier;
    }

    // Shield là buff dạng bật/tắt, không nhân hệ số -> "stack" chỉ để biết
    // còn buff nào đang giữ trạng thái bật hay không.
    public void ShieldBuff()
    {
        if (currentShieldStack == 0)
        {
            playerCollisionHandler.enabled = false;
        }

        if (currentShieldStack >= maxShieldStack)
        {
            return;
        }

        currentShieldStack++;
        PlayEffect(ShieldBuffEffectPrefab, ShieldBuffDuration);
        StartCoroutine(ShieldStackTimeout());
    }

    private IEnumerator ShieldStackTimeout()
    {
        yield return new WaitForSeconds(ShieldBuffDuration);
        if (currentShieldStack > 0)
        {
            currentShieldStack--;
        }

        if (currentShieldStack == 0)
        {
            playerCollisionHandler.enabled = true;
        }
    }

    public void BuffDoubleCoin()
    {
        if (currentCoinStack >= maxCoinStack)
        {
            return;
        }

        currentCoinStack++;
        ApplyCoinByStack();
        PlayEffect(CoinBuffEffectPrefab, CoinBuffDuration);
        StartCoroutine(CoinStackTimeout());
    }

    private IEnumerator CoinStackTimeout()
    {
        yield return new WaitForSeconds(CoinBuffDuration);
        if (currentCoinStack > 0)
        {
            currentCoinStack--;
            ApplyCoinByStack();
        }
    }

    private void ApplyCoinByStack()
    {
        if (currentCoinStack == 0)
        {
            CoinCalculator.Instance.multiplier = 1;
            return;
        }

        CoinCalculator.Instance.multiplier = (int)Mathf.Pow(coinBuffMultiplier, currentCoinStack);
    }
}