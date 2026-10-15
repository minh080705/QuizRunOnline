using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text characterNameText;

    [Tooltip("Nút mở popup chọn nhân vật + màu da (chỉ hiện trên dòng của chính mình). Thay cho hai nút mũi tên cũ.")]
    [SerializeField] private Button changeButton;

    [SerializeField] private Color ownerColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Màu chữ trạng thái")]
    [SerializeField] private Color readyColor = new Color(0.243f, 0.812f, 0.557f);
    [SerializeField] private Color notReadyColor = new Color(0.502f, 0.537f, 0.741f);

    private PlayerRoomData _data;
    private bool _isMine;

    public void Setup(PlayerRoomData data, bool isMine)
    {
        _data = data;
        _isMine = isMine;

        // Chỉ hiện nút đổi nhân vật trên đúng dòng của chính mình
        if (changeButton != null)
        {
            changeButton.gameObject.SetActive(isMine);

            if (isMine)
                changeButton.onClick.AddListener(() => CharacterPickerPopup.Instance?.Open(_data));
        }
    }

    public void Refresh()
    {
        nameText.text = _data.PlayerName;
        statusText.text = _data.IsReady ? "Sẵn sàng" : "Chưa sẵn sàng";
        statusText.color = _data.IsReady ? readyColor : notReadyColor;
        nameText.color = _data.IsRoomOwner ? ownerColor : normalColor;

        // Đã Sẵn sàng thì không đổi nhân vật nữa (SetAppearance cũng chặn).
        if (changeButton != null && _isMine)
            changeButton.interactable = !_data.IsReady;

        characterNameText.text = DescribeAppearance(_data.CharacterIndex, _data.SkinIndex);
    }

    // "Mèo - Cam": tên nhân vật và tên màu da lấy từ CharacterDatabase.
    private static string DescribeAppearance(int characterIndex, int skinIndex)
    {
        CharacterDatabase db = CharacterDatabase.Load();
        CharacterData character = db != null ? db.Get(characterIndex) : null;
        if (character == null) return string.Empty;

        SkinData skin = db.GetSkin(characterIndex, skinIndex);
        return skin != null && !string.IsNullOrEmpty(skin.displayName)
            ? $"{character.displayName} - {skin.displayName}"
            : character.displayName;
    }
}
