using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [SerializeField] private Color ownerColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private PlayerRoomData _data;

    public void Setup(PlayerRoomData data, bool isMine)
    {
        _data = data;

        // Chỉ hiện nút đổi nhân vật trên đúng dòng của chính mình
        prevButton.gameObject.SetActive(isMine);
        nextButton.gameObject.SetActive(isMine);

        if (isMine)
        {
            prevButton.onClick.AddListener(() => _data.ChangeCharacter(-1));
            nextButton.onClick.AddListener(() => _data.ChangeCharacter(1));
        }
    }

    public void Refresh()
    {
        nameText.text = _data.PlayerName;
        statusText.text = _data.IsReady ? "Sẵn sàng" : "Chưa sẵn sàng";
        nameText.color = _data.IsRoomOwner ? ownerColor : normalColor;
        characterNameText.text = CharacterCatalog.Instance.Get(_data.CharacterIndex).displayName;
    }
}