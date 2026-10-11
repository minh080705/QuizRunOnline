using TMPro;
using UnityEngine;

/// <summary>
/// 1 dòng trong bảng xếp hạng. Gắn lên Prefab dòng (Rank + Tên + Điểm).
/// </summary>
public class LeaderboardRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void SetData(int rank, string displayName, int score)
    {
       
        if (nameText != null) nameText.text = displayName;
        if (scoreText != null) scoreText.text = score.ToString();
    }
}