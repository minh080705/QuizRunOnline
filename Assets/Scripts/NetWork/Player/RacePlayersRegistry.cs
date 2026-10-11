using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RacePlayersRegistry : MonoBehaviour
{
    public static RacePlayersRegistry Instance { get; private set; }
    private List<PlayerStats> players = new List<PlayerStats>();


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
        }
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    public void Register(PlayerStats player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }
    }

    public void Unregister(PlayerStats player)
    {
        players.Remove(player);
    }

    // Chỉ đọc cờ mạng khi object còn hợp lệ (tránh đọc state của object đang bị hủy).
    private static bool IsEliminated(PlayerStats p)
    {
        return p.Object != null && p.Object.IsValid && p.IsEliminated;
    }

    public PlayerStats GetLeadingPlayer()
    {
        PlayerStats leading = null;
        float maxZ = float.MinValue;
        foreach (var p in players)
        {
            // Người đã hết mạng nằm lại trên đường: không tính là người dẫn đầu (dùng để sinh vật cản/vạch đích).
            if (p == null || IsEliminated(p)) continue;
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
            // Người đã bị loại nằm lại phía sau sẽ khiến vật cản không bao giờ được dọn, nên bỏ qua.
            if (p == null || IsEliminated(p)) continue;
            if (p.transform.position.z < minZ)
            {
                minZ = p.transform.position.z;
                trailing = p;
            }
        }
        return trailing;
    }

    /// <summary>
    /// Liệt kê toàn bộ player đang đăng ký - dùng cho RaceSpectatorManager
    /// để xây danh sách người có thể theo dõi.
    /// </summary>
    public IReadOnlyList<PlayerStats> GetAllPlayers()
    {
        return players;
    }

    /// <summary>
    /// Tìm PlayerStats tương ứng với 1 PlayerRef - dùng khi chỉ có PlayerRef
    /// (ví dụ từ 1 RPC) và cần tra ra đúng object PlayerStats/Transform.
    /// </summary>
    public PlayerStats GetByPlayerRef(PlayerRef playerRef)
    {
        foreach (var p in players)
        {
            if (p != null && p.Object != null && p.Object.InputAuthority == playerRef)
                return p;
        }
        return null;
    }
}