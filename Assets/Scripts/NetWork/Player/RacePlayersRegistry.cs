using System.Collections.Generic;
using UnityEngine;

public class RacePlayersRegistry : MonoBehaviour
{
    public static RacePlayersRegistry Instance { get; private set; }
    private List<PlayerStats> players = new List<PlayerStats>();
    
  
    void Awake()
    {
        if (Instance == null) Instance = this;
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    public void Register(PlayerStats player)
    {
        if (!players.Contains(player)) players.Add(player);
    }

    public void Unregister(PlayerStats player)
    {
        players.Remove(player);
    }

    public PlayerStats GetLeadingPlayer()
    {
        PlayerStats leading = null;
        float maxZ = float.MinValue;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (p.transform.position.z > maxZ)
            {
                maxZ = p.transform.position.z;
                leading = p;
            }
        }
        return leading;
    }

    public PlayerStats GetTrailingPlayer()
    {
        PlayerStats trailing = null;
        float minZ = float.MaxValue;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (p.transform.position.z < minZ)
            {
                minZ = p.transform.position.z;
                trailing = p;
            }
        }
        return trailing;
    }
}