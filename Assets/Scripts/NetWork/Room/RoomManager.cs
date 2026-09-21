using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : NetworkBehaviour
{

    private Dictionary<PlayerRef, PlayerRoomData> _players = new();


    void Update()
    {
        if (!Runner.IsSharedModeMasterClient) return;

        var allPlayers = FindObjectsOfType<PlayerRoomData>();
        if (allPlayers.Length == 0) return;

        bool allReady = true;
        foreach (var p in allPlayers)
            if (!p.IsReady) { allReady = false; break; }

        if (allReady)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains("RaceScene"))
                {
                    Runner.LoadScene(SceneRef.FromIndex(i));
                    return;
                }
            }

            Debug.LogError("Không tìm thấy RaceScene trong Build Settings!");
        }
    }
}   