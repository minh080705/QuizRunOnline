using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// CRUD hồ sơ người dùng trên Firestore (users/{uid}). Giữ bản cache ở Current.
/// </summary>
public class UserProfileService : MonoBehaviour
{
    public static UserProfileService Instance { get; private set; }

    public UserProfile Current { get; private set; }
    public event Action<UserProfile> OnProfileChanged;

    private DocumentReference doc;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>Đọc hồ sơ của user hiện tại; chưa có thì tạo mới.</summary>
    public async Task<UserProfile> LoadOrCreateAsync()
    {
        AuthService auth = AuthService.Instance;
        if (auth == null || !auth.IsSignedIn) return null;

        if (!BackendMode.UseFirebase)
        {
            // Hồ sơ cục bộ trong bộ nhớ; tên ngẫu nhiên để 2 cửa sổ test không trùng tên.
            Current = new UserProfile
            {
                characterName = "Player_" + UnityEngine.Random.Range(100, 999),
                createdAt = Now(),
                updatedAt = Now(),
            };
            OnProfileChanged?.Invoke(Current);
            return Current;
        }

        doc = FirebaseFirestore.DefaultInstance.Collection("users").Document(auth.UserId);
        var user = auth;

        try
        {
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                Current = snap.ConvertTo<UserProfile>();
            }
            else
            {
                Current = new UserProfile
                {
                    characterName = string.IsNullOrEmpty(user.DisplayName) ? "Player" : user.DisplayName,
                    createdAt = Now(),
                    updatedAt = Now(),
                };
                await doc.SetAsync(Current);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Profile] Tải hồ sơ lỗi: {e}");
            return null;
        }

        OnProfileChanged?.Invoke(Current);
        return Current;
    }

    public async Task<bool> SetCharacterNameAsync(string newName)
    {
        newName = newName?.Trim();
        if (Current == null || string.IsNullOrEmpty(newName)) return false;

        return await UpdateAsync(
            new Dictionary<string, object> { { "characterName", newName } },
            () => Current.characterName = newName);
    }

    /// <summary>Cộng (hoặc trừ nếu âm) coin bằng FieldValue.Increment để không ghi đè sai.</summary>
    public async Task<bool> AddCoinsAsync(int amount)
    {
        if (Current == null) return false;

        return await UpdateAsync(
            new Dictionary<string, object> { { "coins", FieldValue.Increment(amount) } },
            () => Current.coins += amount);
    }

    // =========================================================
    // SỞ HỮU (một chỗ duy nhất quyết định đồ nào "đã có")
    // Giá 0 = mở sẵn; còn lại phải nằm trong danh sách đã mua của hồ sơ.
    // Chưa có hồ sơ (test thẳng Lobby không đăng nhập) thì chỉ đồ miễn phí được coi là có.
    // =========================================================

    public static string SkinKey(int characterIndex, int skinIndex) => $"{characterIndex}:{skinIndex}";

    public bool OwnsCharacter(int characterIndex)
    {
        CharacterDatabase db = CharacterDatabase.Load();
        CharacterData data = db != null ? db.Get(characterIndex) : null;
        if (data == null) return false; // chỉ số không tồn tại

        return data.price <= 0 || (Current != null && Current.ownedCharacters.Contains(characterIndex));
    }

    public bool OwnsSkin(int characterIndex, int skinIndex)
    {
        if (!OwnsCharacter(characterIndex)) return false;

        CharacterDatabase db = CharacterDatabase.Load();
        SkinData skin = db != null ? db.GetSkin(characterIndex, skinIndex) : null;
        if (skin == null) return false;

        return skin.price <= 0 || (Current != null && Current.ownedSkins.Contains(SkinKey(characterIndex, skinIndex)));
    }

    /// <summary>Màu đang chọn của nhân vật; không hợp lệ hoặc chưa sở hữu thì trả 0.</summary>
    public int GetSelectedSkin(int characterIndex)
    {
        if (Current == null || characterIndex < 0 || characterIndex >= Current.selectedSkins.Count) return 0;

        int skin = Current.selectedSkins[characterIndex];
        return OwnsSkin(characterIndex, skin) ? skin : 0;
    }

    // =========================================================
    // CHỌN
    // =========================================================

    /// <summary>Lưu nhân vật đang chọn (chỉ nhận nhân vật đã sở hữu).</summary>
    public async Task<bool> SetSelectedCharacterAsync(int characterIndex)
    {
        if (Current == null || !OwnsCharacter(characterIndex)) return false;

        return await UpdateAsync(
            new Dictionary<string, object> { { "selectedCharacter", characterIndex } },
            () => Current.selectedCharacter = characterIndex);
    }

    /// <summary>Lưu màu đang chọn của một nhân vật (chỉ nhận màu đã sở hữu).</summary>
    public async Task<bool> SetSelectedSkinAsync(int characterIndex, int skinIndex)
    {
        if (Current == null || !OwnsSkin(characterIndex, skinIndex)) return false;

        // Mảng theo chỉ số nhân vật: nới rộng nếu thiếu rồi ghi cả mảng.
        var updated = new List<int>(Current.selectedSkins);
        while (updated.Count <= characterIndex) updated.Add(0);
        updated[characterIndex] = skinIndex;

        return await UpdateAsync(
            new Dictionary<string, object> { { "selectedSkins", updated } },
            () => Current.selectedSkins = updated);
    }

    // =========================================================
    // MUA (trừ xu)
    // =========================================================

    public enum PurchaseResult { Success, NotEnoughCoins, AlreadyOwned, InvalidItem, Failed }

    public Task<PurchaseResult> PurchaseCharacterAsync(int characterIndex)
    {
        CharacterDatabase db = CharacterDatabase.Load();
        CharacterData data = db != null ? db.Get(characterIndex) : null;
        if (Current == null || data == null) return Task.FromResult(PurchaseResult.InvalidItem);

        return PurchaseAsync(
            price: data.price,
            field: "ownedCharacters",
            element: characterIndex,
            isOwned: p => data.price <= 0 || p.ownedCharacters.Contains(characterIndex),
            applyLocal: () => Current.ownedCharacters.Add(characterIndex));
    }

    public Task<PurchaseResult> PurchaseSkinAsync(int characterIndex, int skinIndex)
    {
        CharacterDatabase db = CharacterDatabase.Load();
        SkinData skin = db != null ? db.GetSkin(characterIndex, skinIndex) : null;
        if (Current == null || skin == null) return Task.FromResult(PurchaseResult.InvalidItem);

        // Phải sở hữu nhân vật trước khi mua màu của nhân vật đó.
        if (!OwnsCharacter(characterIndex)) return Task.FromResult(PurchaseResult.InvalidItem);

        string key = SkinKey(characterIndex, skinIndex);
        return PurchaseAsync(
            price: skin.price,
            field: "ownedSkins",
            element: key,
            isOwned: p => skin.price <= 0 || p.ownedSkins.Contains(key),
            applyLocal: () => Current.ownedSkins.Add(key));
    }

    /// <summary>
    /// Mua một món: trên Firestore dùng transaction (đọc hồ sơ mới nhất → kiểm tra đủ xu và chưa sở hữu →
    /// trừ xu + thêm vào danh sách) nên không bị trừ xu hai lần hoặc âm xu khi bấm liên tục / nhiều thiết bị.
    /// </summary>
    private async Task<PurchaseResult> PurchaseAsync(int price, string field, object element,
        Func<UserProfile, bool> isOwned, Action applyLocal)
    {
        if (isOwned(Current)) return PurchaseResult.AlreadyOwned;

        if (!BackendMode.UseFirebase)
        {
            if (Current.coins < price) return PurchaseResult.NotEnoughCoins;

            Current.coins -= price;
            applyLocal();
            Current.updatedAt = Now();
            OnProfileChanged?.Invoke(Current);
            return PurchaseResult.Success;
        }

        if (doc == null) return PurchaseResult.Failed;

        try
        {
            int newCoins = Current.coins;

            PurchaseResult result = await FirebaseFirestore.DefaultInstance.RunTransactionAsync(async tx =>
            {
                DocumentSnapshot snap = await tx.GetSnapshotAsync(doc);
                UserProfile server = snap.ConvertTo<UserProfile>();

                if (isOwned(server)) return PurchaseResult.AlreadyOwned;
                if (server.coins < price) { newCoins = server.coins; return PurchaseResult.NotEnoughCoins; }

                newCoins = server.coins - price;
                tx.Update(doc, new Dictionary<string, object>
                {
                    { "coins", newCoins },
                    { field, FieldValue.ArrayUnion(element) },
                    { "updatedAt", Now() },
                });
                return PurchaseResult.Success;
            });

            // Đồng bộ lại số xu theo server (có thể khác bản cache nếu xu vừa đổi ở nơi khác).
            Current.coins = newCoins;
            if (result == PurchaseResult.Success)
            {
                applyLocal();
                Current.updatedAt = Now();
            }
            OnProfileChanged?.Invoke(Current);
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Profile] Mua hàng lỗi: {e}");
            return PurchaseResult.Failed;
        }
    }

    /// <summary>Quên hồ sơ đang giữ trong bộ nhớ (khi đăng xuất). Không đụng tới dữ liệu trên Firestore.</summary>
    public void Clear()
    {
        Current = null;
        doc = null;
        OnProfileChanged?.Invoke(null);
    }

    /// <summary>Xóa document hồ sơ trên Firestore (chưa xóa tài khoản Auth).</summary>
    public async Task<bool> DeleteProfileAsync()
    {
        if (BackendMode.UseFirebase)
        {
            if (doc == null) return false;
            try
            {
                await doc.DeleteAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Profile] Xóa hồ sơ lỗi: {e}");
                return false;
            }
        }

        Current = null;
        doc = null;
        OnProfileChanged?.Invoke(null);
        return true;
    }

    private async Task<bool> UpdateAsync(Dictionary<string, object> fields, Action applyLocal)
    {
        if (BackendMode.UseFirebase)
        {
            if (doc == null) return false;
            fields["updatedAt"] = Now();

            try
            {
                await doc.UpdateAsync(fields);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Profile] Cập nhật lỗi: {e}");
                return false;
            }
        }

        applyLocal();
        Current.updatedAt = Now();
        OnProfileChanged?.Invoke(Current);
        return true;
    }
}
