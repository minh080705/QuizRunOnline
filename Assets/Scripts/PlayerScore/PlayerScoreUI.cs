using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerScoreUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void OnEnable()
    {
  
        PlayerScoreManager.Instance.OnScoreUpdated += HandleScoreUpdated;
        UpdateScoreText(PlayerScoreManager.Instance.TotalScore);
        
    }

    void OnDisable()
    {
        PlayerScoreManager.Instance.OnScoreUpdated -= HandleScoreUpdated;
    }

    private void HandleScoreUpdated(int gained, int total, ScoreSource source)
    {
        UpdateScoreText(total);
    }

    public void UpdateScoreText(int total)
    {
        if (scoreText != null)
        {
            scoreText.text = total.ToString();
        }
    }
}
