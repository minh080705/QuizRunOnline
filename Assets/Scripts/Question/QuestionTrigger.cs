using Fusion;
using UnityEngine;

/// <summary>Gắn lên mỗi chướng ngại vật chứa câu hỏi: phát hiện va chạm với nhân vật của máy này và nhờ QuestionManager hiển thị câu hỏi.</summary>
public class QuestionTrigger : MonoBehaviour
{
    [Tooltip("Loại câu hỏi prefab này hỏi: Math = toán, English = tiếng Anh, Any = trộn cả hai.")]
    [SerializeField] private QuestionCategory category = QuestionCategory.Any;

    public QuestionCategory Category => category;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Chỉ nhân vật do CHÍNH máy này điều khiển mới được kích hoạt câu hỏi.
        // Bản proxy của người chơi khác (được Fusion replicate sang) cũng mang tag "Player",
        // nếu không lọc thì họ chạm trigger sẽ bật câu hỏi trên màn hình của mình.
        NetworkObject playerObject = other.GetComponentInParent<NetworkObject>();
        if (playerObject == null || !playerObject.HasStateAuthority)
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

    // Được QuestionManager gọi ngược lại sau khi trả lời xong (đúng, sai hoặc hết giờ).
    // Câu hỏi là object CỤC BỘ của máy này và chỉ nhân vật của chính mình mới kích hoạt được (xem OnTriggerEnter),
    // nên ẩn ở đây chỉ ẩn bản trên máy mình; người chơi khác vẫn thấy câu hỏi của họ.

    public void OnAnsweredCorrectly()
    {
        HideAfterAnswer();
    }

    public void OnAnsweredWrong()
    {
        HideAfterAnswer();
    }

    private void HideAfterAnswer()
    {
        Destroy(gameObject);
    }
}