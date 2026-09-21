using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerContext : MonoBehaviour
{
    public static PlayerContext Instance { get; private set; }
    public Transform LocalPlayer { get; private set; }
    public PlayerStats LocalStats { get; private set; }
    public InventoryManager LocalInventory { get; private set; }

    public static event Action<Transform> OnLocalPlayerReady;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RegisterLocalPlayer(Transform player, PlayerStats stats)
    {
        LocalPlayer = player;
        LocalStats = stats;
        LocalInventory = player.GetComponent<InventoryManager>();
        int subscriberCount = OnLocalPlayerReady?.GetInvocationList().Length ?? 0;



        OnLocalPlayerReady?.Invoke(player);
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
