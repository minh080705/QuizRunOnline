using UnityEngine;

/// <summary>
/// Cấu hình điểm thưởng theo thứ tự về đích.
/// Dùng mảng để designer tự thêm/bớt số hạng có thưởng trong Inspector,
/// không cần sửa code khi đổi số lượng hạng.
/// Assets > Create > Score Rules > Rank Bonus Rule
/// </summary>
[CreateAssetMenu(menuName = "Score Rules/Rank Bonus Rule", fileName = "RankBonusRule")]
public class RankBonusRule : ScriptableObject, IScoreRule<int>
{
    [Tooltip("Index 0 = hạng 1, index 1 = hạng 2, v.v. Về đích ngoài danh sách này sẽ nhận defaultBonus.")]
    public int[] bonusByRank = new int[] { 500, 300, 150 };

    public int defaultBonus = 0;

    /// <param name="input">Thứ hạng về đích, tính từ 1 (1 = về nhất)</param>
    public int CalculateScore(int input)
    {
        int index = input - 1;
        if (index >= 0 && index < bonusByRank.Length)
            return bonusByRank[index];

        return defaultBonus;
    }
}