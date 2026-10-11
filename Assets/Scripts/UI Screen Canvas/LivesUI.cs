using TMPro;
using UnityEngine;

/// <summary>
/// HUD số mạng của player local: bật/tắt các icon tim theo số mạng còn lại, và (tùy chọn) hiện chữ đếm ngược
/// hồi sinh / thông báo hết mạng. Đọc trực tiếp PlayerLives.Local mỗi frame (cách DistanceIndicatorManager làm),
/// nên không phụ thuộc thứ tự khởi tạo giữa UI và nhân vật.
/// Đặt dưới canvas HUD của RaceScene.
/// </summary>
public class LivesUI : MonoBehaviour
{
    [Tooltip("Các icon tim theo thứ tự. Số tim đang bật = số mạng còn lại (tim thứ i bật khi còn > i mạng).")]
    [SerializeField] private GameObject[] hearts;

    [Header("Tùy chọn")]
    [Tooltip("Chữ hiện đếm ngược hồi sinh / thông báo hết mạng. Để trống nếu không dùng.")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string respawnFormat = "Hồi sinh sau {0}s";
    [SerializeField] private string eliminatedText = "Bạn đã hết mạng!";

    private int shownLives = -1;

    private void Update()
    {
        PlayerLives lives = PlayerLives.Local;

        // Chưa có nhân vật local: hiện đủ tim, không có chữ.
        int count = lives != null ? lives.Lives : hearts.Length;
        if (count != shownLives)
        {
            shownLives = count;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] != null)
                    hearts[i].SetActive(i < count);
            }
        }

        if (statusText == null) return;

        if (lives == null)
        {
            statusText.text = string.Empty;
        }
        else if (lives.IsEliminated)
        {
            statusText.text = eliminatedText;
        }
        else if (lives.RespawnRemaining > 0f)
        {
            statusText.text = string.Format(respawnFormat, Mathf.CeilToInt(lives.RespawnRemaining));
        }
        else
        {
            statusText.text = string.Empty;
        }
    }
}
