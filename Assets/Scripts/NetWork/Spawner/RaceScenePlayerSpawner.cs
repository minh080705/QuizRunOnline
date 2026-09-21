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
        Debug.Log($"[RaceScenePlayerSpawner] Tìm thấy {allRoomData.Length} PlayerRoomData");

        foreach (var d in allRoomData)
        {
            Debug.Log($"[RaceScenePlayerSpawner] Data: HasStateAuthority={d.Object.HasStateAuthority}, CharacterIndex={d.CharacterIndex}, Name={d.PlayerName}");
        }

        var myRoomData = allRoomData.FirstOrDefault(p => p.Object.HasStateAuthority);
        int characterIndex = LocalPlayerSelection.CharacterIndex;
        Debug.Log($"[RaceScenePlayerSpawner] myRoomData null? {myRoomData == null}, characterIndex CHỌN = {characterIndex}");

        NetworkPrefabRef prefabToSpawn = CharacterCatalog.Instance.Get(characterIndex).prefab;

        // 2. Xác định spawn point không trùng nhau
        var sortedPlayers = runner.ActivePlayers.OrderBy(p => p.PlayerId).ToList();
        int spawnIndex = sortedPlayers.IndexOf(runner.LocalPlayer) % spawnPoints.Length;
        Vector3 pos = spawnPoints[spawnIndex].position;

        // 3. Spawn nhân vật chạy thật
        runner.Spawn(prefabToSpawn, pos, Quaternion.identity, runner.LocalPlayer);

       
    }
}