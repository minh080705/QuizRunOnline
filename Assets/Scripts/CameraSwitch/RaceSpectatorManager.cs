using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Theo dõi danh sách người chơi CÒN ĐANG ĐUA (chưa về đích), cho phép
/// chuyển đổi (Next/Previous) người đang được camera theo dõi.
/// Chỉ chứa DỮ LIỆU + EVENT, không biết gì về Camera hay UI cụ thể.
/// </summary>
public class RaceSpectatorManager : MonoBehaviour
{
    public static RaceSpectatorManager Instance { get; private set; }

    private readonly HashSet<PlayerRef> finishedPlayers = new HashSet<PlayerRef>();
    private readonly List<PlayerStats> activeTargets = new List<PlayerStats>();
    private int currentIndex = -1;

    /// Bắn ra khi target đang theo dõi đổi. Null = không còn ai để theo dõi.
    public event Action<Transform> OnSpectateTargetChanged;

    /// Bắn ra đúng 1 lần khi spectator mode được kích hoạt.
    public event Action OnSpectatorModeActivated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Gọi từ FinishLine (broadcast tới mọi máy) mỗi khi có 1 player về đích.
    /// PlayerRef ở đây là người đã spawn nhân vật đó (= StateAuthority của nhân vật).
    /// </summary>
    public void MarkPlayerFinished(PlayerRef player)
    {
        finishedPlayers.Add(player);

        if (currentIndex < 0)
            return; // Spectator mode chưa kích hoạt trên máy này.

        PlayerStats currentlyFollowing = (currentIndex < activeTargets.Count)
            ? activeTargets[currentIndex]
            : null;

        RebuildActiveTargets();

        if (activeTargets.Count == 0)
        {
            currentIndex = -1;
            OnSpectateTargetChanged?.Invoke(null);
            return;
        }

        // Tìm lại đúng người đang xem trong danh sách MỚI (không giữ số index cũ)
        int newIndex = currentlyFollowing != null ? activeTargets.IndexOf(currentlyFollowing) : -1;

        if (newIndex >= 0)
        {
            currentIndex = newIndex; // vẫn xem người cũ, không cần báo đổi target
        }
        else
        {
            currentIndex = 0;        // người đang xem vừa về đích -> sang người đầu danh sách
            NotifyTargetChanged();
        }
    }

    /// <summary>
    /// Gọi khi CHÍNH player local đã về đích - bắt đầu theo dõi người khác.
    /// </summary>
    public void ActivateSpectatorMode()
    {
        RebuildActiveTargets();
        currentIndex = activeTargets.Count > 0 ? 0 : -1;


        OnSpectatorModeActivated?.Invoke();
        NotifyTargetChanged();
    }

    public void NextTarget()
    {
        if (currentIndex < 0) return; // spectator chưa kích hoạt / không còn ai

        activeTargets.RemoveAll(p => p == null); // dọn người đã thoát (object bị hủy)
        if (activeTargets.Count == 0)
        {
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, activeTargets.Count - 1);
        currentIndex = (currentIndex + 1) % activeTargets.Count;
        NotifyTargetChanged();
    }

    public void PreviousTarget()
    {
        if (currentIndex < 0) return;

        activeTargets.RemoveAll(p => p == null);
        if (activeTargets.Count == 0)
        {
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, activeTargets.Count - 1);
        currentIndex = (currentIndex - 1 + activeTargets.Count) % activeTargets.Count;
        NotifyTargetChanged();
    }

    private void RebuildActiveTargets()
    {
        activeTargets.Clear();

        if (RacePlayersRegistry.Instance == null)
        {
            return;
        }

        foreach (PlayerStats p in RacePlayersRegistry.Instance.GetAllPlayers())
        {
            if (p == null || p.Object == null) continue;
            if (p.Object.HasStateAuthority) continue; // bỏ qua nhân vật của chính mình

            if (!finishedPlayers.Contains(p.Object.StateAuthority))
                activeTargets.Add(p);
            Debug.Log($"{p.name}: Input={p.Object.InputAuthority}, State={p.Object.StateAuthority}");
        }

    }

    private void NotifyTargetChanged()
    {
        if (currentIndex < 0 || currentIndex >= activeTargets.Count)
        {
            OnSpectateTargetChanged?.Invoke(null);
            return;
        }

        PlayerStats target = activeTargets[currentIndex];
        OnSpectateTargetChanged?.Invoke(target != null ? target.transform : null);
    }
}