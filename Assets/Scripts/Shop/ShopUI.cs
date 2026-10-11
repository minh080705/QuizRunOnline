using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ShopScene: mua nhân vật và màu da bằng xu. Hai tab (Nhân vật / Màu da); màu da thuộc từng nhân vật nên
/// tab Màu da có bộ chọn nhân vật. Dữ liệu giá lấy từ CharacterDatabase, trạng thái sở hữu và xu từ
/// UserProfileService (hồ sơ đã được AccountScene nạp; AuthGate trong scene đẩy về Account nếu chưa đăng nhập).
///
/// Đặt component này trên một object LUÔN BẬT; chỉ hộp xác nhận được bật/tắt.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Chung")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button backButton;

    [Header("Lưới thẻ")]
    [SerializeField] private Transform grid;
    [SerializeField] private ShopItemCard cardPrefab;

    [Header("Tab")]
    [SerializeField] private Button characterTabButton;
    [SerializeField] private Button skinTabButton;

    [Tooltip("Bật để tô màu tab đang chọn (đổi màu ảnh nền và chữ của nút). Tắt thì giữ nguyên màu bạn đặt trong scene.")]
    [SerializeField] private bool highlightActiveTab;
    [SerializeField] private Color tabActiveColor = new Color(1f, 0.784f, 0.239f);
    [SerializeField] private Color tabInactiveColor = new Color(0.149f, 0.169f, 0.322f);
    [SerializeField] private Color tabActiveTextColor = new Color(0.227f, 0.165f, 0f);
    [SerializeField] private Color tabInactiveTextColor = new Color(0.667f, 0.69f, 0.847f);

    [Header("Chọn nhân vật cho tab Màu da")]
    [Tooltip("Cả khối (mũi tên + tên) này chỉ hiện ở tab Màu da.")]
    [SerializeField] private GameObject skinCharacterSelector;
    [SerializeField] private Button prevCharacterButton;
    [SerializeField] private Button nextCharacterButton;
    [SerializeField] private TMP_Text skinCharacterNameText;

    [Header("Hộp xác nhận mua")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    private enum Tab { Characters, Skins }

    private Tab tab = Tab.Characters;
    private int skinCharacterIndex;
    private Action pendingPurchase;
    private bool busy;

    private void Awake()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        SetStatus(string.Empty);
    }

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Menu));
        if (characterTabButton != null) characterTabButton.onClick.AddListener(() => SwitchTab(Tab.Characters));
        if (skinTabButton != null) skinTabButton.onClick.AddListener(() => SwitchTab(Tab.Skins));
        if (prevCharacterButton != null) prevCharacterButton.onClick.AddListener(() => StepSkinCharacter(-1));
        if (nextCharacterButton != null) nextCharacterButton.onClick.AddListener(() => StepSkinCharacter(1));
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CloseConfirm);

        Refresh();
    }

    private void OnEnable()
    {
        if (UserProfileService.Instance != null)
            UserProfileService.Instance.OnProfileChanged += HandleProfileChanged;
    }

    private void OnDisable()
    {
        if (UserProfileService.Instance != null)
            UserProfileService.Instance.OnProfileChanged -= HandleProfileChanged;
    }

    private void HandleProfileChanged(UserProfile profile)
    {
        Refresh();
    }

    // ---------------- Tab ----------------

    private void SwitchTab(Tab newTab)
    {
        tab = newTab;
        SetStatus(string.Empty);
        Refresh();
    }

    private void StepSkinCharacter(int direction)
    {
        CharacterDatabase db = CharacterDatabase.Load();
        if (db == null || db.CharacterCount == 0) return;

        skinCharacterIndex = db.Wrap(skinCharacterIndex + direction);
        Refresh();
    }

    // ---------------- Hiển thị ----------------

    private void StyleTab(Button tabButton, bool active)
    {
        if (tabButton == null) return;

        if (tabButton.image != null)
            tabButton.image.color = active ? tabActiveColor : tabInactiveColor;

        TMP_Text label = tabButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.color = active ? tabActiveTextColor : tabInactiveTextColor;
    }

    private void Refresh()
    {
        if (highlightActiveTab)
        {
            StyleTab(characterTabButton, tab == Tab.Characters);
            StyleTab(skinTabButton, tab == Tab.Skins);
        }

        CharacterDatabase db = CharacterDatabase.Load();
        UserProfileService profiles = UserProfileService.Instance;
        if (db == null || profiles == null || grid == null) return;

        if (coinText != null)
            coinText.text = profiles.Current != null ? $"{profiles.Current.coins}" : "0";

        if (skinCharacterSelector != null)
            skinCharacterSelector.SetActive(tab == Tab.Skins);

        ShopItemCard.ClearGrid(grid);

        if (tab == Tab.Characters)
            BuildCharacterCards(db, profiles);
        else
            BuildSkinCards(db, profiles);
    }

    private void BuildCharacterCards(CharacterDatabase db, UserProfileService profiles)
    {
        for (int c = 0; c < db.CharacterCount; c++)
        {
            int index = c;
            CharacterData character = db.Get(c);

            CardState state = ResolveState(profiles.OwnsCharacter(c), character.price, profiles);
            ShopItemCard card = Instantiate(cardPrefab, grid);
            card.Setup(character.displayName, character.icon, character.price, state,
                () => AskToBuy(character.displayName, character.price,
                               () => profiles.PurchaseCharacterAsync(index)));
        }
    }

    private void BuildSkinCards(CharacterDatabase db, UserProfileService profiles)
    {
        if (db.CharacterCount == 0) return;
        skinCharacterIndex = db.Wrap(skinCharacterIndex);

        CharacterData character = db.Get(skinCharacterIndex);
        if (skinCharacterNameText != null) skinCharacterNameText.text = character.displayName;

        bool ownsCharacter = profiles.OwnsCharacter(skinCharacterIndex);

        for (int s = 0; s < db.SkinCount(skinCharacterIndex); s++)
        {
            int characterIndex = skinCharacterIndex;
            int index = s;
            SkinData skin = db.GetSkin(characterIndex, s);

            // Chưa có nhân vật thì chưa mua được màu của nhân vật đó.
            CardState state = ownsCharacter
                ? ResolveState(profiles.OwnsSkin(characterIndex, s), skin.price, profiles)
                : CardState.Locked;

            ShopItemCard card = Instantiate(cardPrefab, grid);
            card.Setup(skin.displayName, skin.icon, skin.price, state,
                () => AskToBuy(skin.displayName, skin.price,
                               () => profiles.PurchaseSkinAsync(characterIndex, index)));
        }
    }

    private static CardState ResolveState(bool owned, int price, UserProfileService profiles)
    {
        if (owned) return CardState.Owned;

        int coins = profiles.Current != null ? profiles.Current.coins : 0;
        return coins >= price ? CardState.Buy : CardState.NotEnoughCoins;
    }

    // ---------------- Mua ----------------

    private void AskToBuy(string itemName, int price, Func<System.Threading.Tasks.Task<UserProfileService.PurchaseResult>> purchase)
    {
        if (busy) return;

        pendingPurchase = async () =>
        {
            busy = true;
            CloseConfirm();

            UserProfileService.PurchaseResult result = await purchase();
            SetStatus(DescribeResult(result, itemName));

            busy = false;
            Refresh(); // làm mới thẻ và số xu (kể cả khi thất bại, vì xu có thể đã đổi theo server)
        };

        if (confirmPanel == null)
        {
            // Không có hộp xác nhận trong scene: mua luôn.
            pendingPurchase();
            return;
        }

        if (confirmText != null) confirmText.text = $"Mua {itemName} với giá {price} xu?";
        confirmPanel.SetActive(true);
    }

    private void OnConfirmYes()
    {
        Action run = pendingPurchase;
        pendingPurchase = null;
        run?.Invoke();
    }

    private void CloseConfirm()
    {
        pendingPurchase = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private static string DescribeResult(UserProfileService.PurchaseResult result, string itemName)
    {
        switch (result)
        {
            case UserProfileService.PurchaseResult.Success: return $"Đã mua {itemName}!";
            case UserProfileService.PurchaseResult.NotEnoughCoins: return "Không đủ xu.";
            case UserProfileService.PurchaseResult.AlreadyOwned: return "Bạn đã sở hữu món này.";
            case UserProfileService.PurchaseResult.InvalidItem: return "Món này không mua được.";
            default: return "Mua thất bại, thử lại sau.";
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
