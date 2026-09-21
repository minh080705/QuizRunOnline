using UnityEngine;

public class MapSpawner : MonoBehaviour
{
    [Header("Map Prefabs")]
    public GameObject[] maps = new GameObject[5];

    Vector3 spawnPosition = Vector3.zero;
    Vector3 posCheck = Vector3.zero;
    Transform player;

    private GameObject currentMap;
    private GameObject lastMap;

    void OnEnable()
    {
        PlayerContext.OnLocalPlayerReady += HandlePlayerReady;
       
    }

    void OnDisable()
    {
        PlayerContext.OnLocalPlayerReady -= HandlePlayerReady;
      
    }

    void HandlePlayerReady(Transform localPlayer)
    {
       
        player = localPlayer;
        posCheck = player.position;
        spawnPosition = player.position + Vector3.forward * 250;
        ShowNewMap();
    }

    void ShowNewMap()
    {
        lastMap = currentMap;
        int randomIndex = Random.Range(0, maps.Length);
        currentMap = Instantiate(maps[randomIndex], spawnPosition, maps[randomIndex].transform.rotation);

        posCheck = spawnPosition - Vector3.forward * 250;
        spawnPosition += new Vector3(0, 0, 500);
    }

    void Update()
    {
        if (player == null) return; // chưa có player thì chưa làm gì

        if (player.position.z - posCheck.z > 250)
        {
            Destroy(lastMap, 0f);
            ShowNewMap();
        }
    }
}