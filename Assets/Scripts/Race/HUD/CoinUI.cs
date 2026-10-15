using UnityEngine;
using TMPro; // nếu dùng TextMeshPro

public class CoinUI : MonoBehaviour
{
    public TextMeshProUGUI coinText;

    void OnEnable()
    {
        CoinCalculator.OnCoinChanged += UpdateCoinText;
    }

    void OnDisable()
    {
        CoinCalculator.OnCoinChanged -= UpdateCoinText;
    }

    void Start()
    {
        // Hiển thị giá trị hiện tại ngay khi vào scene
        if (CoinCalculator.Instance != null)
            UpdateCoinText(CoinCalculator.Instance.GetTotalCoin());
    }

    void UpdateCoinText(int newTotal)
    {
        coinText.text = newTotal.ToString();
    }
}