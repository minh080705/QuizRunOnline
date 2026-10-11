using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một chấm trên thanh tiến trình (1 chấm / player). Chỉ lo hiển thị, không biết gì về mạng.
/// Gắn trên prefab UI "ProgressMarker"; RaceProgressBarUI tạo và di chuyển các chấm này.
/// </summary>
public class ProgressMarkerUI : MonoBehaviour
{
    [SerializeField] private Image dot;

    [Tooltip("Vòng quanh chấm (con của prefab), chỉ bật cho player local. Để trống nếu không muốn đánh dấu.")]
    [SerializeField] private GameObject localRing;

    [SerializeField] private Color aliveColor = Color.black;
    [SerializeField] private Color deadColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    public RectTransform Rect => (RectTransform)transform;

    private bool isLocal;
    private bool isDead;

    private void Awake()
    {
        Apply();
    }

    public void SetLocal(bool local)
    {
        if (isLocal == local) return;
        isLocal = local;
        Apply();
    }

    public void SetDead(bool dead)
    {
        if (isDead == dead) return;
        isDead = dead;
        Apply();
    }

    private void Apply()
    {
        // Mọi chấm cùng kích thước; player local chỉ khác ở vòng trắng quanh chấm.
        if (localRing != null)
            localRing.SetActive(isLocal);

        if (dot != null)
            dot.color = isDead ? deadColor : aliveColor;
    }
}
