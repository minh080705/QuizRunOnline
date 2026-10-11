using Fusion;
using UnityEngine;

public class PlayerRoomData : NetworkBehaviour
{
    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkBool IsRoomOwner { get; set; }
    [Networked] public int CharacterIndex { get; set; }
    [Networked] public int SkinIndex { get; set; }
    [Networked, Capacity(16)] public string PlayerName { get; set; }
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsRoomOwner = Runner.IsSharedModeMasterClient;
            // Lấy tên + nhân vật + màu từ hồ sơ đăng nhập; không có hồ sơ (vào thẳng LobbyScene) thì dùng mặc định.
            UserProfileService profiles = UserProfileService.Instance;
            UserProfile profile = profiles != null ? profiles.Current : null;
            if (profile != null)
            {
                string name = profile.characterName ?? "Player";
                PlayerName = name.Length > 16 ? name.Substring(0, 16) : name;

                int character = profiles.OwnsCharacter(profile.selectedCharacter) ? profile.selectedCharacter : 0;
                CharacterIndex = character;
                SkinIndex = profiles.GetSelectedSkin(character);
            }
            else
            {
                PlayerName = "Player_" + Random.Range(100, 999);
                CharacterIndex = 0;
                SkinIndex = 0;
            }

            LocalPlayerSelection.CharacterIndex = CharacterIndex;
            LocalPlayerSelection.SkinIndex = SkinIndex;
        }
    }

    public void SetReady(bool ready)
    {
        if (!Object.HasStateAuthority) return;
        IsReady = ready;
    }

    /// <summary>
    /// Đổi nhân vật + màu (từ popup chọn trong Lobby; popup chỉ liệt kê đồ đã sở hữu). Không đổi được khi đã Sẵn sàng.
    /// Việc lưu lựa chọn vào hồ sơ do popup đảm nhận; ở đây chỉ đồng bộ cho cả phòng.
    /// </summary>
    public void SetAppearance(int characterIndex, int skinIndex)
    {
        if (!Object.HasStateAuthority || IsReady) return;

        CharacterIndex = characterIndex;
        SkinIndex = skinIndex;

        LocalPlayerSelection.CharacterIndex = CharacterIndex; // ← lưu cục bộ
        LocalPlayerSelection.SkinIndex = SkinIndex;
    }
}
