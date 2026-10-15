using System.Collections.Generic;
using Firebase.Firestore;

/// <summary>
/// Hồ sơ người dùng lưu tại Firestore: users/{uid}.
/// ownedCharacters là index theo CharacterCatalog.
/// </summary>
[FirestoreData]
public class UserProfile
{
    [FirestoreProperty] public string characterName { get; set; } = "Player";
    [FirestoreProperty] public int coins { get; set; } = 0;
    [FirestoreProperty] public List<int> ownedCharacters { get; set; } = new List<int> { 0 };
    [FirestoreProperty] public int selectedCharacter { get; set; } = 0;

    // Màu da đã mua, định dạng "nhânVật:màu" (ví dụ "0:2"). Giá 0 là mở sẵn nên không cần nằm trong danh sách.
    [FirestoreProperty] public List<string> ownedSkins { get; set; } = new List<string>();

    // Màu đang chọn của từng nhân vật, theo chỉ số nhân vật (thiếu phần tử thì coi là 0).
    [FirestoreProperty] public List<int> selectedSkins { get; set; } = new List<int>();
    [FirestoreProperty] public long createdAt { get; set; }
    [FirestoreProperty] public long updatedAt { get; set; }
}
