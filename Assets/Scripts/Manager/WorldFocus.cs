using System;
using UnityEngine;

/// <summary>
/// "Tâm của thế giới": nhân vật mà map, đường ray, vật cản, coin, item được dựng quanh nó.
/// Mặc định là nhân vật local. Khi camera chuyển sang theo dõi người khác (người chơi hết mạng hoặc đã về đích), tâm
/// chuyển theo người đang xem để quanh họ luôn có đủ thế giới. Khi local lên bục trao giải thì quay về nhân vật local
/// (thế giới quanh vạch đích và bục).
///
/// Là một component nhỏ tự tạo lúc cần (EnsureExists, gọi từ các spawner), không phải đặt tay vào scene.
/// Chỉ là dữ liệu và sự kiện: spawner đăng ký OnTargetChanged và đọc Target (hủy đăng ký ở OnDisable).
/// </summary>
public class WorldFocus : MonoBehaviour
{
    public static WorldFocus Instance { get; private set; }

    /// <summary>Nhân vật làm tâm thế giới hiện tại. Null khi chưa có nhân vật local.</summary>
    public static Transform Target { get; private set; }

    /// <summary>Bắn khi Target đổi (kể cả từ null sang có giá trị).</summary>
    public static event Action<Transform> OnTargetChanged;

    private Transform localPlayer;
    private Transform spectateTarget;
    private bool spectating;
    private bool lockedToLocal; // đã lên bục: giữ thế giới quanh vạch đích, không theo người khác nữa

    private bool subscribedSpectator;
    private bool subscribedPodium;
    private HandlePodiumPosArrange podium;
    private float nextPodiumSearch;

    /// <summary>Tạo WorldFocus nếu scene chưa có. An toàn khi gọi nhiều lần.</summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("WorldFocus");
        go.AddComponent<WorldFocus>(); // Awake gán Instance
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        PlayerContext.OnLocalPlayerReady += HandleLocalPlayerReady;

        // Nhân vật local có thể đã sẵn sàng trước khi component này được tạo.
        if (PlayerContext.Instance != null && PlayerContext.Instance.LocalPlayer != null)
            HandleLocalPlayerReady(PlayerContext.Instance.LocalPlayer);
    }

    private void OnDisable()
    {
        PlayerContext.OnLocalPlayerReady -= HandleLocalPlayerReady;

        if (subscribedSpectator && RaceSpectatorManager.Instance != null)
            RaceSpectatorManager.Instance.OnSpectateTargetChanged -= HandleSpectateTargetChanged;
        subscribedSpectator = false;

        if (subscribedPodium && podium != null)
            podium.OnLocalPlayerReachedPodium -= HandlePodiumReached;
        subscribedPodium = false;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        Target = null; // static: không để lại tham chiếu tới object của scene cũ
    }

    private void Update()
    {
        // RaceSpectatorManager và HandlePodiumPosArrange là scene object, có thể xuất hiện sau: đăng ký lười (giống CameraFollow).
        if (!subscribedSpectator && RaceSpectatorManager.Instance != null)
        {
            RaceSpectatorManager.Instance.OnSpectateTargetChanged += HandleSpectateTargetChanged;
            subscribedSpectator = true;
        }

        if (!subscribedPodium && Time.unscaledTime >= nextPodiumSearch)
        {
            nextPodiumSearch = Time.unscaledTime + 1f;
            podium = FindObjectOfType<HandlePodiumPosArrange>(true);
            if (podium != null)
            {
                podium.OnLocalPlayerReachedPodium += HandlePodiumReached;
                subscribedPodium = true;
            }
        }

        // Người đang xem thoát phòng (object bị hủy): quay về nhân vật local.
        if (spectating && spectateTarget == null)
        {
            spectating = false;
            SetTarget(localPlayer);
        }
    }

    private void HandleLocalPlayerReady(Transform player)
    {
        localPlayer = player;
        if (!spectating) SetTarget(localPlayer);
    }

    private void HandleSpectateTargetChanged(Transform target)
    {
        if (lockedToLocal) return;

        if (target != null)
        {
            spectating = true;
            spectateTarget = target;
            SetTarget(target);
        }
        else
        {
            // Không còn ai để xem: về nhân vật local.
            spectating = false;
            spectateTarget = null;
            SetTarget(localPlayer);
        }
    }

    private void HandlePodiumReached(Transform championPos, Transform runnerupPos, Transform thirdplacePos)
    {
        lockedToLocal = true;
        spectating = false;
        spectateTarget = null;
        SetTarget(localPlayer);
    }

    private static void SetTarget(Transform target)
    {
        if (Target == target) return;

        Target = target;
        OnTargetChanged?.Invoke(target);
    }
}
