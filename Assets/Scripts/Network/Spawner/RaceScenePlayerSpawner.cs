using System.Linq;
using Fusion;
using UnityEngine;

public class RaceScenePlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    void Start()
    {
        NetworkRunner runner = LobbyManager.Instance.Runner;

        var allRoomData = FindObjectsOfType<PlayerRoomData>();
        var myRoomData = allRoomData.FirstOrDefault(p => p.Object.HasStateAuthority);
        // Ưu tiên giá trị đã đồng bộ trong PlayerRoomData (lấy từ hồ sơ ở lobby); fallback về lựa chọn cục bộ.
        int characterIndex = myRoomData != null ? myRoomData.CharacterIndex : LocalPlayerSelection.CharacterIndex;
        int skinIndex = myRoomData != null ? myRoomData.SkinIndex : LocalPlayerSelection.SkinIndex;

        NetworkPrefabRef prefabToSpawn = CharacterCatalog.Instance.Get(characterIndex).prefab;

        var sortedPlayers = runner.ActivePlayers.OrderBy(p => p.PlayerId).ToList();

        // Chỉ một người trong phòng: xuất phát ở điểm giữa đường (X gần 0 nhất) thay vì SpawnPoint_0 lệch sang trái.
        // Nhiều người: giữ cách chia theo thứ tự PlayerId như cũ.
        int spawnIndex = sortedPlayers.Count <= 1
            ? GetCenterSpawnIndex()
            : sortedPlayers.IndexOf(runner.LocalPlayer) % spawnPoints.Length;
        Vector3 pos = spawnPoints[spawnIndex].position;

        // Gửi chỉ số nhân vật + màu kèm dữ liệu mạng của nhân vật để MỌI máy áp đúng màu da (xem CharacterSkin).
        runner.Spawn(prefabToSpawn, pos, Quaternion.identity, runner.LocalPlayer,
            onBeforeSpawned: (spawnRunner, spawnedObject) =>
            {
                CharacterSkin skin = spawnedObject.GetComponentInChildren<CharacterSkin>();
                if (skin != null)
                    skin.SetAppearance(characterIndex, skinIndex);
            });
    }

    // Chỉ số điểm spawn nằm gần giữa đường nhất (|X| nhỏ nhất); bằng nhau thì lấy điểm đứng trước.
    private int GetCenterSpawnIndex()
    {
        int best = 0;
        float bestAbsX = float.MaxValue;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            float absX = Mathf.Abs(spawnPoints[i].position.x);
            if (absX < bestAbsX)
            {
                bestAbsX = absX;
                best = i;
            }
        }
        return best;
    }
}