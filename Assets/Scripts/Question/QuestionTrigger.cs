// QuestionTrigger.cs
// Gắn script này lên MỖI chướng ngại vật chứa câu hỏi.
// Script chỉ chịu trách nhiệm:
// - Phát hiện va chạm với Player
// - Nhớ trạng thái riêng: đã được trả lời/kích hoạt hay chưa
// - Gọi sang QuestionManager (singleton) để hiển thị câu hỏi
//
// Không chứa UI, không chứa JSON -> nhẹ, dễ nhân bản ra nhiều object.

using UnityEngine;

public class QuestionTrigger : MonoBehaviour
{
    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isTriggered)
            return;

        if (QuestionManager.Instance == null)
        {
            Debug.LogError("Không tìm thấy QuestionManager.Instance trong scene.");
            return;
        }

        // Chỉ khóa trigger nếu Manager thực sự chấp nhận hiển thị câu hỏi.
        // Nếu đang có câu hỏi khác chưa trả lời, Manager trả về false
        // -> trigger này KHÔNG bị khóa, player có thể chạm lại sau để hỏi tiếp.
        bool accepted = QuestionManager.Instance.ShowQuestion(this, other.gameObject);

        if (accepted)
        {
            isTriggered = true;
        }
    }

    // Được QuestionManager gọi ngược lại sau khi trả lời xong
    // (nếu muốn xử lý riêng cho từng chướng ngại vật, ví dụ: phá hủy object, đổi màu, v.v.)

    public void OnAnsweredCorrectly()
    {
        // TODO: xử lý riêng khi trả lời đúng, ví dụ:
        // gameObject.SetActive(false);
    }

    public void OnAnsweredWrong()
    {
        // TODO: xử lý riêng khi trả lời sai, ví dụ:
        // cho phép hỏi lại bằng cách đặt isTriggered = false;
    }
}