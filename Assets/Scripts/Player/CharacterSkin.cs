using Fusion;
using UnityEngine;

/// <summary>
/// Áp màu da (material) lên nhân vật trong RaceScene, trên MỌI máy. Gắn trên prefab nhân vật (cùng NetworkObject).
///
/// Chỉ số nhân vật + màu được gửi qua dữ liệu mạng (đặt trong onBeforeSpawned của Runner.Spawn, trước khi object
/// được replicate), nên các máy khác thấy đúng màu ngay khi nhân vật xuất hiện.
/// Dùng sharedMaterial (không phải .material) để các nhân vật cùng màu dùng chung một material, không tạo bản sao.
/// </summary>
public class CharacterSkin : NetworkBehaviour
{
    [Tooltip("Renderer của phần da (ví dụ CatMesh).")]
    [SerializeField] private SkinnedMeshRenderer targetRenderer;

    [Tooltip("Vị trí material cần thay trong renderer (thường là 0).")]
    [SerializeField] private int materialSlot = 0;

    [Networked] public int CharacterIndex { get; private set; }
    [Networked] public int SkinIndex { get; private set; }
    [Networked] private NetworkBool HasAppearance { get; set; }

    /// <summary>Gọi từ onBeforeSpawned của Runner.Spawn.</summary>
    public void SetAppearance(int characterIndex, int skinIndex)
    {
        CharacterIndex = characterIndex;
        SkinIndex = skinIndex;
        HasAppearance = true;
    }

    public override void Spawned()
    {
        Apply();
    }

    private void Apply()
    {
        if (!HasAppearance || targetRenderer == null) return;

        CharacterDatabase db = CharacterDatabase.Load();
        SkinData skin = db != null ? db.GetSkin(CharacterIndex, SkinIndex) : null;
        if (skin == null || skin.material == null) return; // chỉ số sai: giữ material mặc định của prefab

        Material[] materials = targetRenderer.sharedMaterials;
        if (materialSlot < 0 || materialSlot >= materials.Length) return;

        materials[materialSlot] = skin.material;
        targetRenderer.sharedMaterials = materials;
    }
}
