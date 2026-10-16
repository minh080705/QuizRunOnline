using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup trong Lobby để chọn nhân vật + màu da. CHỈ liệt kê đồ đã sở hữu, không có mua bán
/// (muốn mua phải về AccountScene -> Shop). Mở từ nút "Đổi nhân vật" trên hàng của chính mình (PlayerRowUI).
///
/// Component này nằm trên một object LUÔN BẬT (để Awake chạy và Instance có sẵn); chỉ `root` được bật/tắt.
/// </summary>
public class CharacterPickerPopup : MonoBehaviour
{
    public static CharacterPickerPopup Instance { get; private set; }

    [Tooltip("Phần hiển thị của popup (con của object này), mặc định tắt.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Transform characterGrid;
    [SerializeField] private Transform skinGrid;
    [SerializeField] private ShopItemCard cardPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Tooltip("Dòng gợi ý, ví dụ: Muốn mua thêm nhân vật/màu: về Tài khoản > Shop (tùy chọn).")]
    [SerializeField] private TMP_Text hintText;

    private PlayerRoomData data;
    private int pendingCharacter;
    private int pendingSkin;

    private void Awake()
    {
        Instance = this;
        if (root != null) root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (hintText != null && string.IsNullOrEmpty(hintText.text))
            hintText.text = "Muốn mua thêm nhân vật/màu da: về Tài khoản > Shop.";
    }

    /// <summary>Mở popup cho PlayerRoomData của chính mình.</summary>
    public void Open(PlayerRoomData playerData)
    {
        if (playerData == null || playerData.IsReady) return;

        data = playerData;
        pendingCharacter = playerData.CharacterIndex;
        pendingSkin = playerData.SkinIndex;

        Rebuild();
        root.SetActive(true);
    }

    public void Close()
    {
        root.SetActive(false);
        data = null;
    }

    private void Rebuild()
    {
        CharacterDatabase db = CharacterDatabase.Load();
        UserProfileService profiles = UserProfileService.Instance;
        if (db == null || profiles == null) return;

        ShopItemCard.ClearGrid(characterGrid);
        ShopItemCard.ClearGrid(skinGrid);

        for (int c = 0; c < db.CharacterCount; c++)
        {
            if (!profiles.OwnsCharacter(c)) continue;

            int index = c;
            CharacterData character = db.Get(c);
            ShopItemCard card = Instantiate(cardPrefab, characterGrid);
            card.Setup(character.displayName, character.icon, 0,
                index == pendingCharacter ? CardState.Selected : CardState.Pick,
                () =>
                {
                    pendingCharacter = index;
                    pendingSkin = profiles.GetSelectedSkin(index); // về màu đã chọn trước đó của nhân vật này
                    Rebuild();
                });
        }

        for (int s = 0; s < db.SkinCount(pendingCharacter); s++)
        {
            if (!profiles.OwnsSkin(pendingCharacter, s)) continue;

            int index = s;
            SkinData skin = db.GetSkin(pendingCharacter, s);
            ShopItemCard card = Instantiate(cardPrefab, skinGrid);
            card.Setup(skin.displayName, skin.icon, 0,
                index == pendingSkin ? CardState.Selected : CardState.Pick,
                () =>
                {
                    pendingSkin = index;
                    Rebuild();
                });
        }
    }

    private async void OnConfirm()
    {
        if (data == null) { Close(); return; }

        // Đồng bộ cho cả phòng (SetAppearance chặn nếu đã Sẵn sàng)
        data.SetAppearance(pendingCharacter, pendingSkin);

        // Lưu vào hồ sơ để lần sau vào lại vẫn nhớ
        UserProfileService profiles = UserProfileService.Instance;
        int character = pendingCharacter;
        int skin = pendingSkin;
        Close();

        if (profiles != null && profiles.Current != null)
        {
            await profiles.SetSelectedCharacterAsync(character);
            await profiles.SetSelectedSkinAsync(character, skin);
        }
    }
}
