using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CardState
{
    Pick,           // popup Lobby: món đã sở hữu, bấm để chọn
    Selected,       // popup Lobby: món đang được chọn
    Owned,          // Shop: đã sở hữu
    Buy,            // Shop: chưa có, đủ xu -> bấm để mua
    NotEnoughCoins, // Shop: chưa có, thiếu xu
    Locked,         // Shop: màu da của nhân vật chưa mua
}

/// <summary>
/// Thẻ ảnh 2D của một nhân vật / một màu da (icon, tên, giá, nút). Dùng chung cho Shop và popup chọn trong Lobby.
/// Chỉ hiển thị, không biết gì về mua bán: người tạo thẻ truyền trạng thái và hành động khi bấm.
/// </summary>
public class ShopItemCard : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text buttonText;

    [Tooltip("Khung/dấu tích hiện khi thẻ đang được chọn (tùy chọn).")]
    [SerializeField] private GameObject selectedMark;

    // Unity gọi khi thêm component trong Editor: tự gán các ô theo tên object con.
    private void Reset()
    {
        Transform t;
        t = transform.Find("Icon"); if (t != null) icon = t.GetComponent<Image>();
        t = transform.Find("Name"); if (t != null) nameText = t.GetComponent<TMP_Text>();
        t = transform.Find("Price"); if (t != null) priceText = t.GetComponent<TMP_Text>();
        t = transform.Find("Button");
        if (t != null)
        {
            button = t.GetComponent<Button>();
            buttonText = t.GetComponentInChildren<TMP_Text>();
        }
        t = transform.Find("SelectedMark"); if (t != null) selectedMark = t.gameObject;
    }

    public void Setup(string displayName, Sprite sprite, int price, CardState state, Action onClick)
    {
        if (nameText != null) nameText.text = displayName;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        bool interactable;
        string priceLabel;
        string buttonLabel;

        switch (state)
        {
            case CardState.Pick:
                priceLabel = string.Empty; buttonLabel = "Chọn"; interactable = true; break;
            case CardState.Selected:
                priceLabel = string.Empty; buttonLabel = "Đang chọn"; interactable = false; break;
            case CardState.Owned:
                priceLabel = "Đã sở hữu"; buttonLabel = "Đã có"; interactable = false; break;
            case CardState.Buy:
                priceLabel = $"{price} xu"; buttonLabel = "Mua"; interactable = true; break;
            case CardState.NotEnoughCoins:
                priceLabel = $"{price} xu"; buttonLabel = "Thiếu xu"; interactable = false; break;
            default: // Locked
                priceLabel = string.Empty; buttonLabel = "Cần nhân vật"; interactable = false; break;
        }

        if (priceText != null) priceText.text = priceLabel;
        if (buttonText != null) buttonText.text = buttonLabel;
        if (selectedMark != null) selectedMark.SetActive(state == CardState.Selected);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = interactable;
            if (interactable && onClick != null)
                button.onClick.AddListener(() => onClick());
        }
    }

    /// <summary>Xóa hết thẻ trong một lưới (tắt trước rồi Destroy để layout không tính thẻ cũ).</summary>
    public static void ClearGrid(Transform grid)
    {
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            GameObject child = grid.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }
}
