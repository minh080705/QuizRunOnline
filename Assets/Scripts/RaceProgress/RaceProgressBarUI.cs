using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thanh tiến trình đường đua: mỗi player là một chấm chạy từ vạch xuất phát (trái) tới vạch đích (phải).
/// Chỉ hiển thị: đọc vị trí các player từ RacePlayersRegistry (đã được Fusion đồng bộ sẵn) và quy đổi
/// sang tỉ lệ quãng đường bằng RaceTrackConfig.Progress01, không có code mạng riêng.
///
/// Đặt dưới canvas HUD của RaceScene (không đặt trong GameManager / DontDestroyOnLoad).
/// </summary>
public class RaceProgressBarUI : MonoBehaviour
{
    [Header("Tham chiếu UI")]
    [Tooltip("RectTransform rỗng nằm trên thanh vàng: mép trái = vạch xuất phát, mép phải = chân cột cờ (vạch đích). Các chấm là con của nó.")]
    [SerializeField] private RectTransform trackArea;

    [SerializeField] private ProgressMarkerUI markerPrefab;

    private class Entry
    {
        public ProgressMarkerUI marker;
        public PlayerNetworkState netState; // có thì dùng IsDead đã đồng bộ qua mạng
        public float maxProgress;           // chỉ tăng, không giảm
    }

    private readonly Dictionary<PlayerStats, Entry> entries = new Dictionary<PlayerStats, Entry>();
    private readonly HashSet<PlayerStats> seen = new HashSet<PlayerStats>();
    private readonly List<PlayerStats> toRemove = new List<PlayerStats>();
    private bool warnedMissingConfig;

    private void Awake()
    {
        if (trackArea == null || markerPrefab == null)
        {
            Debug.LogError("[RaceProgressBarUI] Chưa gán trackArea hoặc markerPrefab.");
            enabled = false;
        }
    }

    // LateUpdate: các script khác đã di chuyển nhân vật trong Update, vị trí lấy ra là mới nhất.
    private void LateUpdate()
    {
        RaceTrackConfig track = RaceTrackConfig.Instance;
        RacePlayersRegistry registry = RacePlayersRegistry.Instance;

        if (track == null)
        {
            if (!warnedMissingConfig)
            {
                warnedMissingConfig = true;
                Debug.LogError("[RaceProgressBarUI] Không có RaceTrackConfig trong scene, không biết quãng đường đua.");
            }
            return;
        }
        if (registry == null) return;

        float width = trackArea.rect.width;
        seen.Clear();

        IReadOnlyList<PlayerStats> players = registry.GetAllPlayers();
        for (int i = 0; i < players.Count; i++)
        {
            PlayerStats p = players[i];
            if (p == null || p.Object == null || !p.Object.IsValid) continue;

            seen.Add(p);

            if (!entries.TryGetValue(p, out Entry entry))
            {
                entry = CreateEntry(p);
                entries[p] = entry;
            }

            // Chốt tiến độ chỉ tăng: người về đích dính ở cờ, và không bị kéo ngược khi
            // HandlePodiumPosArrange teleport họ lên bục (Z lúc đó có thể nhỏ hơn vạch đích).
            entry.maxProgress = Mathf.Max(entry.maxProgress, track.Progress01(p.transform.position.z));

            // Mọi chấm cùng nằm trên đường giữa của thanh (y = 0), chỉ khác vị trí x.
            entry.marker.Rect.anchoredPosition = new Vector2(entry.maxProgress * width, 0f);

            bool isLocal = p.Object.HasStateAuthority;
            entry.marker.SetLocal(isLocal);
            entry.marker.SetDead(entry.netState != null ? entry.netState.IsDead : p.isDead);

            // Chấm của mình vẽ trên cùng (chỉ đổi thứ tự khi cần để khỏi làm bẩn canvas mỗi frame).
            if (isLocal)
            {
                Transform t = entry.marker.transform;
                if (t.GetSiblingIndex() != t.parent.childCount - 1)
                    t.SetAsLastSibling();
            }
        }

        RemoveMissing();
    }

    private Entry CreateEntry(PlayerStats p)
    {
        return new Entry
        {
            marker = Instantiate(markerPrefab, trackArea),
            netState = p.GetComponentInParent<PlayerNetworkState>(),
            maxProgress = 0f,
        };
    }

    // Player đã rời phòng / bị despawn thì bỏ chấm tương ứng.
    private void RemoveMissing()
    {
        toRemove.Clear();
        foreach (var kvp in entries)
        {
            if (!seen.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            Entry entry = entries[toRemove[i]];
            if (entry.marker != null)
                Destroy(entry.marker.gameObject);
            entries.Remove(toRemove[i]);
        }
    }
}
