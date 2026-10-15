using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Nhãn tên trên đầu nhân vật (thay cho DistanceIndicatorManager).
/// - Khi ĐUA: hiện "Tên - 25m" trên đầu người chơi khác (không hiện của chính mình);
///   gần hơn showDistanceFrom thì chỉ hiện tên, càng xa chữ càng nhỏ nhưng không nhỏ hơn minFontSize.
/// - Khi đã đứng trên BỤC (Podium): hiện tên của tất cả người đứng trên bục, kể cả người chơi local,
///   mỗi bục một màu riêng (nhất / nhì / ba), không kèm khoảng cách.
/// Nhãn được tạo bằng code dưới canvasRect (Screen Space - Overlay), không cần prefab.
/// </summary>
public class PlayerNameTagManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Canvas chứa các nhãn (Screen Space - Overlay). Bỏ trống thì dùng chính object này.")]
    [SerializeField] private RectTransform canvasRect;
    [Tooltip("Font TMP cho nhãn. Bỏ trống thì dùng font mặc định của TMP.")]
    [SerializeField] private TMP_FontAsset font;
    [Tooltip("Bục vinh quang trong RaceScene. Bỏ trống thì tự tìm HandlePodiumPosArrange trong scene.")]
    [SerializeField] private HandlePodiumPosArrange podium;

    [Header("Vị trí hiển thị")]
    [Tooltip("Chiều cao phía trên gốc nhân vật (mét) nơi đặt nhãn.")]
    [SerializeField] private float heightOffset = 2.2f;
    [Tooltip("Khoảng cách tối thiểu từ nhãn tới mép màn hình khi nhân vật nằm ngoài khung hình (pixel).")]
    [SerializeField] private float edgePadding = 20f;

    [Header("Trên bục")]
    [Tooltip("Nhân vật cách vị trí một bục trong bán kính này (mét) thì coi là đang đứng trên bục đó.")]
    [SerializeField] private float podiumRadius = 3f;
    [SerializeField] private float podiumFontSize = 40f;
    [SerializeField] private Color championColor = new Color(1f, 0.84f, 0f, 1f);
    [SerializeField] private Color runnerUpColor = new Color(0.82f, 0.86f, 0.92f, 1f);
    [SerializeField] private Color thirdPlaceColor = new Color(0.85f, 0.52f, 0.22f, 1f);

    [Header("Khi đua")]
    [Tooltip("Từ khoảng cách này trở lên mới hiện thêm ' - xx m'; gần hơn thì chỉ hiện tên.")]
    [SerializeField] private float showDistanceFrom = 10f;
    [Tooltip("Gần hơn mức này: chữ to nhất.")]
    [SerializeField] private float nearDistance = 10f;
    [Tooltip("Xa hơn mức này: chữ giữ nguyên cỡ nhỏ nhất (không nhỏ thêm).")]
    [SerializeField] private float farDistance = 120f;
    [SerializeField] private float maxFontSize = 36f;
    [Tooltip("Cỡ chữ nhỏ nhất - đặt đủ lớn để luôn nhìn thấy được.")]
    [SerializeField] private float minFontSize = 18f;
    [SerializeField] private Color aliveColor = Color.white;
    [Tooltip("Màu của người đã chết / hết mạng (chỉ dùng khi đang đua).")]
    [SerializeField] private Color deadColor = new Color(1f, 1f, 1f, 0.35f);

    [Header("Viền chữ")]
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.25f;

    private const float RetryInterval = 1f;
    private const int RaceMode = -1; // Tag.Mode: -1 = đang đua, 0..2 = đang ở bục tương ứng

    private class Tag
    {
        public TextMeshProUGUI Text;
        public RectTransform Rect;
        public string Name;
        public bool NameResolved;
        public float NextNameTry;
        public int Mode = -2;           // -2 = chưa vẽ lần nào
        public int LastDistance = -2;   // -1 = chỉ hiện tên
        public int LastFontSize = -1;
        public bool LastDead;
        public bool Dirty = true;
    }

    private readonly Dictionary<PlayerStats, Tag> _tags = new();
    private readonly List<PlayerStats> _toRemove = new();
    private readonly HashSet<PlayerStats> _seen = new();
    private Canvas _canvas;
    private float _nextPodiumSearch;

    private void Awake()
    {
        if (canvasRect == null) canvasRect = (RectTransform)transform;
        _canvas = canvasRect.GetComponentInParent<Canvas>();
    }

    private void LateUpdate()
    {
        Transform local = PlayerContext.Instance != null ? PlayerContext.Instance.LocalPlayer : null;
        Camera cam = Camera.main;
        RacePlayersRegistry registry = RacePlayersRegistry.Instance;
        if (local == null || cam == null || registry == null) return;

        FindPodiumIfNeeded();

        _seen.Clear();
        // Duyệt theo chỉ số (không foreach): GetAllPlayers trả IReadOnlyList nên foreach sẽ tạo enumerator mới mỗi frame (rác GC).
        IReadOnlyList<PlayerStats> players = registry.GetAllPlayers();
        for (int i = 0; i < players.Count; i++)
        {
            PlayerStats p = players[i];
            if (p == null || p.Object == null || !p.Object.IsValid) continue;

            int slot = podium != null ? podium.GetPodiumSlot(p.transform.position, podiumRadius) : -1;
            bool isLocal = p.transform == local;
            if (slot < 0 && isLocal) continue; // đang đua: không hiện tên của chính mình

            _seen.Add(p);
            if (!_tags.TryGetValue(p, out Tag tag))
            {
                tag = CreateTag();
                _tags[p] = tag;
            }
            UpdateTag(tag, p, local, cam, slot);
        }

        // Dọn nhãn của người đã rời / bị hủy / không còn thuộc diện hiện tên.
        _toRemove.Clear();
        foreach (var kv in _tags)
            if (kv.Key == null || !_seen.Contains(kv.Key)) _toRemove.Add(kv.Key);
        foreach (PlayerStats key in _toRemove)
        {
            if (_tags.TryGetValue(key, out Tag t) && t.Rect != null) Destroy(t.Rect.gameObject);
            _tags.Remove(key);
        }
    }

    // Bục có thể đang tắt trong scene lúc khởi động (FinishLineSpawner bật nó lên sau), nên tìm lười, kể cả object inactive.
    private void FindPodiumIfNeeded()
    {
        if (podium != null || Time.unscaledTime < _nextPodiumSearch) return;
        _nextPodiumSearch = Time.unscaledTime + RetryInterval;
        podium = FindObjectOfType<HandlePodiumPosArrange>(true);
    }

    private Tag CreateTag()
    {
        var go = new GameObject("PlayerNameTag", typeof(RectTransform));
        go.transform.SetParent(canvasRect, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.fontStyle = FontStyles.Bold;
        text.outlineWidth = outlineWidth;
        text.outlineColor = outlineColor;

        return new Tag { Text = text, Rect = (RectTransform)go.transform };
    }

    private Color PodiumColor(int slot)
    {
        return slot == 0 ? championColor : slot == 1 ? runnerUpColor : thirdPlaceColor;
    }

    private void UpdateTag(Tag tag, PlayerStats target, Transform local, Camera cam, int slot)
    {
        TryResolveName(tag, target);

        bool onPodium = slot >= 0;
        int mode = onPodium ? slot : RaceMode;

        int shownDistance = -1;
        bool dead = false;
        int fontSize;
        if (onPodium)
        {
            fontSize = Mathf.RoundToInt(podiumFontSize);
        }
        else
        {
            float distance = Vector3.Distance(local.position, target.transform.position);
            shownDistance = distance >= showDistanceFrom ? Mathf.RoundToInt(distance) : -1;
            dead = target.isDead || target.IsEliminated;
            float t01 = Mathf.InverseLerp(nearDistance, farDistance, distance);
            fontSize = Mathf.RoundToInt(Mathf.Lerp(maxFontSize, minFontSize, t01));
        }

        if (tag.Dirty || mode != tag.Mode || shownDistance != tag.LastDistance
            || fontSize != tag.LastFontSize || dead != tag.LastDead)
        {
            tag.Text.text = shownDistance >= 0 ? $"{tag.Name} - {shownDistance}m" : tag.Name;
            tag.Text.fontSize = fontSize;

            Color color = onPodium ? PodiumColor(slot) : (dead ? deadColor : aliveColor);
            tag.Text.color = color;
            tag.Text.outlineColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a * color.a);
            tag.Rect.sizeDelta = new Vector2(tag.Text.preferredWidth, tag.Text.preferredHeight);

            tag.Mode = mode;
            tag.LastDistance = shownDistance;
            tag.LastFontSize = fontSize;
            tag.LastDead = dead;
            tag.Dirty = false;
        }

        // Khi đua: nhãn ghim vào mép màn hình nếu nhân vật ngoài khung hình. Trên bục: ngoài khung hình thì ẩn.
        bool visible = TryGetScreenPosition(tag, target.transform.position + Vector3.up * heightOffset, cam,
            !onPodium, out Vector3 screenPos);
        if (tag.Rect.gameObject.activeSelf != visible) tag.Rect.gameObject.SetActive(visible);
        if (visible) tag.Rect.position = screenPos;
    }

    private bool TryGetScreenPosition(Tag tag, Vector3 worldPos, Camera cam, bool pinToEdge, out Vector3 result)
    {
        result = default;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        bool behind = screenPos.z < 0f;

        // Sau camera: WorldToScreenPoint cho điểm bị lật, nên phản chiếu qua tâm để ra đúng hướng.
        Vector2 raw = behind
            ? center - (new Vector2(screenPos.x, screenPos.y) - center)
            : new Vector2(screenPos.x, screenPos.y);

        // Chừa chỗ cho nửa nhãn để nhãn ghim ở mép không bị cắt.
        float scale = _canvas != null ? _canvas.scaleFactor : 1f;
        float padX = edgePadding + tag.Rect.sizeDelta.x * scale * 0.5f;
        float padY = edgePadding + tag.Rect.sizeDelta.y * scale * 0.5f;

        bool offscreen = behind
            || raw.x < padX || raw.x > Screen.width - padX
            || raw.y < padY || raw.y > Screen.height - padY;
        if (!offscreen)
        {
            result = new Vector3(raw.x, raw.y, 0f);
            return true;
        }
        if (!pinToEdge) return false;

        Vector2 dir = raw - center;
        if (dir == Vector2.zero)
        {
            result = new Vector3(center.x, center.y, 0f);
            return true;
        }

        Vector2 half = new Vector2(Mathf.Max(0f, Screen.width / 2f - padX), Mathf.Max(0f, Screen.height / 2f - padY));
        float tx = dir.x != 0f ? half.x / Mathf.Abs(dir.x) : float.MaxValue;
        float ty = dir.y != 0f ? half.y / Mathf.Abs(dir.y) : float.MaxValue;
        Vector2 pinned = center + dir * Mathf.Min(tx, ty);
        result = new Vector3(pinned.x, pinned.y, 0f);
        return true;
    }

    // Tên lấy từ PlayerRoomData (đồng bộ từ lobby); chưa có thì thử bảng tên của LeaderboardManager.
    // Chưa tìm được thì hiện "Player N" và thử lại mỗi giây.
    private void TryResolveName(Tag tag, PlayerStats target)
    {
        if (tag.NameResolved || Time.unscaledTime < tag.NextNameTry) return;
        tag.NextNameTry = Time.unscaledTime + RetryInterval;

        PlayerRef owner = target.Object.InputAuthority;
        string resolved = null;

        foreach (PlayerRoomData data in FindObjectsOfType<PlayerRoomData>())
        {
            if (data.Object == null || !data.Object.IsValid) continue;
            if (data.Object.InputAuthority != owner && data.Object.StateAuthority != owner) continue;
            if (!string.IsNullOrEmpty(data.PlayerName)) resolved = data.PlayerName;
            break;
        }

        if (resolved == null && LeaderboardManager.Instance != null
            && LeaderboardManager.Instance.TryGetName(owner, out string s)
            && !string.IsNullOrEmpty(s))
        {
            resolved = s;
        }

        if (resolved != null)
        {
            tag.NameResolved = true;
            if (resolved != tag.Name) { tag.Name = resolved; tag.Dirty = true; }
        }
        else if (tag.Name == null)
        {
            tag.Name = $"Player {owner.PlayerId}";
            tag.Dirty = true;
        }
    }
}
