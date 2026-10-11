using UnityEngine;

/// <summary>
/// Cầu nối giữ nguyên API cũ (Get/Wrap) cho code Lobby/Race, nhưng dữ liệu nay nằm trong asset CharacterDatabase
/// (đọc được từ mọi scene, kể cả ShopScene). Thêm nhân vật/màu: sửa asset, không sửa component này.
/// </summary>
public class CharacterCatalog : MonoBehaviour
{
    public static CharacterCatalog Instance;

    [Tooltip("Để trống thì tự nạp Assets/Resources/CharacterDatabase.asset.")]
    [SerializeField] private CharacterDatabase database;

    private CharacterDatabase Db => database != null ? database : CharacterDatabase.Load();

    void Awake() => Instance = this;

    public CharacterData Get(int index) => Db.Get(Wrap(index));

    public int Wrap(int index) => Db != null ? Db.Wrap(index) : 0;
}
