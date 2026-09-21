using UnityEngine;
using TMPro; // nếu dùng TextMeshPro

public class CoinUI : MonoBehaviour
{
    public TextMeshProUGUI coinText;

    void OnEnable()
    {
        CoinCaculator.OnCoinChanged += UpdateCoinText;
    }

    void OnDisable()
    {
        CoinCaculator.OnCoinChanged -= UpdateCoinText;
    }

    void Start()
    {
        // Hiển thị giá trị hiện tại ngay khi vào scene
        if (CoinCaculator.Instance != null)
            UpdateCoinText(CoinCaculator.Instance.GetTotalCoin());
    }

    void UpdateCoinText(int newTotal)
    {
        coinText.text = newTotal.ToString();
    }
}