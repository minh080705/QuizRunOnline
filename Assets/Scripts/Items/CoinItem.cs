using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class CoinItem : MonoBehaviour
{
    [SerializeField] int coinValueSet = 1;
    public int coinValue => coinValueSet;

    private void OnTriggerEnter(Collider other)
    {
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

        CoinCaculator.Instance.UpdateTotalCoin(coinValue);

        Destroy(gameObject);
        PlayerScoreManager.Instance?.AddCoinScore(coinValue);
    }
}