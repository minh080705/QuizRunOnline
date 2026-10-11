using System;
using Fusion;
using UnityEngine;

/// <summary>Một màu da của nhân vật: một material đã tạo sẵn trong Assets.</summary>
[Serializable]
public class SkinData
{
    public string displayName;
    public Sprite icon;
    [Tooltip("Giá bằng xu. 0 = mở sẵn.")]
    public int price;
    [Tooltip("Material gán lên renderer của nhân vật khi chọn màu này.")]
    public Material material;
}

[Serializable]
public class CharacterData
{
    public string displayName;
    public Sprite icon;
    [Tooltip("Giá bằng xu. 0 = mở sẵn.")]
    public int price;
    public NetworkPrefabRef prefab;
    [Tooltip("Các màu da. Phần tử 0 nên có giá 0 (màu mặc định).")]
    public SkinData[] skins;
}

/// <summary>
/// Danh sách nhân vật + màu da + giá. ScriptableObject đặt tại Assets/Resources/CharacterDatabase.asset để
/// MỌI scene (Account, Shop, Lobby, Race) đọc được bằng CharacterDatabase.Load(), không phụ thuộc object trong scene.
/// Thêm nhân vật/màu mới chỉ cần thêm phần tử trong asset.
/// </summary>
[CreateAssetMenu(menuName = "Shop/Character Database", fileName = "CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    private const string ResourceName = "CharacterDatabase";
    private static CharacterDatabase cached;

    [SerializeField] private CharacterData[] characters;

    public static CharacterDatabase Load()
    {
        if (cached == null)
        {
            cached = Resources.Load<CharacterDatabase>(ResourceName);
            if (cached == null)
                Debug.LogError("[CharacterDatabase] Không tìm thấy Assets/Resources/CharacterDatabase.asset (Create > Shop > Character Database).");
        }
        return cached;
    }

    public int CharacterCount => characters != null ? characters.Length : 0;

    /// <summary>Trả null nếu chỉ số ngoài phạm vi.</summary>
    public CharacterData Get(int characterIndex)
    {
        if (characters == null || characterIndex < 0 || characterIndex >= characters.Length) return null;
        return characters[characterIndex];
    }

    public int SkinCount(int characterIndex)
    {
        CharacterData c = Get(characterIndex);
        return c != null && c.skins != null ? c.skins.Length : 0;
    }

    /// <summary>Trả null nếu nhân vật hoặc màu không tồn tại.</summary>
    public SkinData GetSkin(int characterIndex, int skinIndex)
    {
        CharacterData c = Get(characterIndex);
        if (c == null || c.skins == null || skinIndex < 0 || skinIndex >= c.skins.Length) return null;
        return c.skins[skinIndex];
    }

    /// <summary>Đưa chỉ số về trong phạm vi (vòng lặp, chịu được số âm).</summary>
    public int Wrap(int index)
    {
        int len = CharacterCount;
        return len == 0 ? 0 : ((index % len) + len) % len;
    }
}
