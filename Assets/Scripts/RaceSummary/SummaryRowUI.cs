using TMPro;
using UnityEngine;

/// <summary>
/// Một hàng trong bảng tổng kết trận (prefab SummaryRow): tên, tổng điểm, số lần chết, số xu.
/// Chỉ hiển thị, không biết gì về mạng. RaceSummaryUI tạo hàng và gọi Setup().
/// </summary>
public class SummaryRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text coinsText;

    [Tooltip("Nền tô sáng, chỉ bật cho hàng của chính người chơi này.")]
    [SerializeField] private GameObject highlight;

    // Unity gọi khi thêm component trong Editor: tự gán các ô theo tên object con để khỏi kéo thả tay.
    private void Reset()
    {
        nameText = FindText("Name");
        scoreText = FindText("Score");
        deathsText = FindText("Deaths");
        coinsText = FindText("Coins");

        Transform h = transform.Find("Highlight");
        highlight = h != null ? h.gameObject : null;
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    public void Setup(string playerName, int score, int deaths, int coins, bool isLocal)
    {
        if (nameText != null) nameText.text = playerName;
        if (scoreText != null) scoreText.text = score.ToString();
        if (deathsText != null) deathsText.text = deaths.ToString();
        if (coinsText != null) coinsText.text = coins.ToString();

        if (highlight != null) highlight.SetActive(isLocal);
    }
}
