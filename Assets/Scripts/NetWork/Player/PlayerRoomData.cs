using Fusion;
using UnityEngine;

public class PlayerRoomData : NetworkBehaviour
{
    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkBool IsRoomOwner { get; set; }
    [Networked] public int CharacterIndex { get; set; }
    [Networked, Capacity(16)] public string PlayerName { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            PlayerName = "Player_" + Random.Range(100, 999);
            IsRoomOwner = Runner.IsSharedModeMasterClient;
            CharacterIndex = 0;
        }
    }

    public void SetReady(bool ready)
    {
        if (!Object.HasStateAuthority) return;
        IsReady = ready;
    }

    public void ChangeCharacter(int direction)
    {
        if (!Object.HasStateAuthority) return;
        CharacterIndex = CharacterCatalog.Instance.Wrap(CharacterIndex + direction);
        LocalPlayerSelection.CharacterIndex = CharacterIndex; // ← lưu cục bộ
    }
}