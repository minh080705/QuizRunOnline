using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class CoinItem : MonoBehaviour
{
    [SerializeField] int coinValueSet = 1;
    public int coinValue => coinValueSet;

    [Header("Hiệu ứng khi nhặt")]
    [Tooltip("Prefab hiệu ứng hạt phát MỘT LẦN và đi theo nhân vật khi người chơi của mình nhặt (tùy chọn, bỏ trống thì không có hiệu ứng). " +
             "Trong prefab hiệu ứng: tắt Looping, Stop Action = None.")]
    [SerializeField] private ParticleSystem pickupEffect;
    [Tooltip("Độ lệch của hiệu ứng so với chân nhân vật (theo trục thế giới), ví dụ (0, 1, 0) là ngang người. " +
             "Cũng là điểm coin bay tới khi bị nam châm hút.")]
    [SerializeField] private Vector3 effectOffset = new Vector3(0f, 1f, 0f);

    [Header("Nam châm (Coin Buff)")]
    [Tooltip("Coin bay tới gần nhân vật trong khoảng này (mét) thì được nhặt luôn, không chờ va chạm vật lý.")]
    [SerializeField] private float magnetCollectDistance = 0.8f;
    [Tooltip("Hệ số tăng tốc khi bay: tốc độ nhân thêm (1 + thời gian bay × hệ số) để luôn đuổi kịp nhân vật đang chạy nhanh.")]
    [SerializeField] private float magnetAcceleration = 2f;

    private bool collected;

    // ---- Danh sách mọi coin đang hoạt động (để CoinMagnet tìm coin trong phạm vi mà không cần Physics) ----
    private static readonly List<CoinItem> active = new List<CoinItem>();
    public static IReadOnlyList<CoinItem> Active => active;

    // ---- Trạng thái bị nam châm hút ----
    private bool attracted;
    private Transform magnetTarget;
    private PlayerStats magnetStats;
    private float magnetBaseSpeed;
    private float flyTime;

    public bool IsAttracted => attracted;

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    /// <summary>
    /// Cho coin bay tới target (nhân vật của MÌNH) rồi tự được nhặt. Chỉ CoinMagnet gọi, mà CoinMagnet chỉ tồn tại ở máy
    /// của người dùng Coin Buff, nên chỉ ảnh hưởng bản coin trên máy đó (coin là object cục bộ từng máy).
    /// </summary>
    public void AttractTo(Transform target, float baseSpeed)
    {
        if (collected || attracted || target == null)
            return;

        attracted = true;
        magnetTarget = target;
        magnetStats = target.GetComponent<PlayerStats>();
        magnetBaseSpeed = baseSpeed;
        flyTime = 0f;

        // Các script hoạt ảnh trên coin (SimpleGemsAnim, ObstacleAnimator...) ghi đè vị trí mỗi frame
        // nên phải tắt, nếu không coin bị kéo ngược về chỗ cũ.
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour != this) behaviour.enabled = false;
        }
    }

    private void Update()
    {
        if (!attracted || collected || magnetTarget == null)
            return;

        // Tốc độ = tốc độ cơ bản + tốc độ chạy hiện tại của nhân vật, rồi tăng dần theo thời gian bay.
        flyTime += Time.deltaTime;
        float speed = magnetBaseSpeed + (magnetStats != null ? magnetStats.moveSpeed : 0f);
        speed *= 1f + flyTime * magnetAcceleration;

        Vector3 aim = magnetTarget.position + effectOffset;
        transform.position = Vector3.MoveTowards(transform.position, aim, speed * Time.deltaTime);

        // Nhặt bằng khoảng cách (không dựa vào trigger vật lý): nhân vật chạy rất nhanh nên trigger dễ bị bỏ lỡ.
        if ((aim - transform.position).sqrMagnitude <= magnetCollectDistance * magnetCollectDistance)
            CollectBy(magnetTarget);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Destroy chỉ có hiệu lực cuối frame: chặn cộng xu và phát hiệu ứng đúp nếu nhiều collider của player cùng chạm.
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        // ---- Kiểm tra Input Authority ----
        // other có thể là player do CHÍNH máy này điều khiển, hoặc là "ghost"
        // của player khác được Fusion replicate sang để hiển thị hình ảnh.
        // Nếu không kiểm tra, cả 2 trường hợp đều khớp tag "Player" và đều
        // kích hoạt OnTriggerEnter TRÊN MÁY NÀY, dẫn tới cộng điểm nhầm khi
        // người chơi KHÁC đi ngang qua coin (dù người đó không hề nhặt được
        // gì trên máy của chính họ).
        //
        // HasInputAuthority == true nghĩa là network object này thuộc quyền
        // điều khiển của chính client đang chạy đoạn code này -> chỉ cộng
        // điểm trong trường hợp này.
        NetworkObject playerNetworkObject = other.GetComponent<NetworkObject>();

        if (playerNetworkObject == null || !playerNetworkObject.HasInputAuthority)
        {
            return;
        }

        CollectBy(other.transform);
    }

    // Dùng chung cho nhặt bằng va chạm và nhặt bằng nam châm. Gọi sau khi đã chắc đây là nhân vật của mình.
    private void CollectBy(Transform player)
    {
        if (collected)
            return;

        collected = true;
        CoinCalculator.Instance.UpdateTotalCoin(coinValue);

        // Hiệu ứng đi theo nhân vật vừa nhặt; không là con của coin nên vẫn chạy khi coin bị Destroy.
        SpawnEffect.PlayFollowing(pickupEffect, player, effectOffset);

        Destroy(gameObject);
        PlayerScoreManager.Instance?.AddCoinScore(coinValue);
    }
}
