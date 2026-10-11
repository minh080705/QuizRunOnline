using System;
using System.Collections;
using Fusion;
using UnityEngine;

public class HandlePodiumPosArrange : MonoBehaviour
{
    [SerializeField] private Transform championPos;
    [SerializeField] private Transform runnerupPos;
    [SerializeField] private Transform thirdplacePos;

    [Header("Đồng bộ điểm")]
    [SerializeField] private float scoreSyncDelay = 1.5f;

    [Header("Chờ nhân vật dừng hẳn")]
    [SerializeField] private float restSpeedThreshold = 0.02f;
    [SerializeField] private int restFixedFrames = 5;
    [SerializeField] private float maxWaitForRest = 5f;

    /// <summary>
    /// Bắn ra sau khi LOCAL player đã được Teleport lên đúng bục của họ.
    /// Tham số: vị trí 3 bục, để camera tự tính điểm lia tới mà không cần
    /// tham chiếu ngược lại HandlePodiumPosArrange.
    /// </summary>
    public event Action<Transform, Transform, Transform> OnLocalPlayerReachedPodium;

    /// <summary>
    /// Bắn ra khi routine sắp xếp bục của máy này đã xong, Ở MỌI NHÁNH: lên bục xong, hoặc không có chỗ trên bục
    /// (hạng ≥ 4), hoặc không tìm thấy nhân vật. Khác OnLocalPlayerReachedPodium (chỉ bắn cho người lên bục).
    /// </summary>
    public event Action OnArrangeCompleted;

    private Transform[] podium;
    private Coroutine arrangeRoutine;

    private void Awake()
    {
        podium = new[] { championPos, runnerupPos, thirdplacePos };
    }

    private void OnEnable()
    {
        RaceResultManager.OnRaceFinish += HandleRaceFinish;
    }

    private void OnDisable()
    {
        RaceResultManager.OnRaceFinish -= HandleRaceFinish;

        if (arrangeRoutine != null)
        {
            StopCoroutine(arrangeRoutine);
            arrangeRoutine = null;
        }
    }

    /// <summary>
    /// Vị trí này đang ở bục nào: 0 = nhất, 1 = nhì, 2 = ba, -1 = không ở bục nào (cách mọi bục quá radius).
    /// Đọc Transform trực tiếp mỗi lần gọi nên vẫn đúng khi podium bị dời tới vạch đích.
    /// </summary>
    public int GetPodiumSlot(Vector3 position, float radius)
    {
        Transform[] slots = { championPos, runnerupPos, thirdplacePos };
        int best = -1;
        float bestSqr = radius * radius;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            float sqr = (slots[i].position - position).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = i;
            }
        }
        return best;
    }

    private void HandleRaceFinish()
    {
        if (arrangeRoutine != null) return;
        arrangeRoutine = StartCoroutine(ArrangeRoutine());
    }

    private IEnumerator ArrangeRoutine()
    {
        yield return new WaitForSeconds(scoreSyncDelay);

        int rank = GetMyRankByScore();
        Debug.Log($"[Podium] my rank index = {rank}");

        if (rank < 0 || rank >= podium.Length || podium[rank] == null)
        {
            Debug.LogWarning("[Podium] không có vị trí bục cho rank này, bỏ qua");
            FinishArrange();
            yield break;
        }

        NetworkObject no = GetLocalPlayerRoot();
        if (no == null)
        {
            Debug.LogWarning("[Podium] không tìm thấy nhân vật local");
            FinishArrange();
            yield break;
        }

        yield return WaitUntilAtRest(no.transform);

        MoveToPodiumPosition(no, podium[rank]);

        // Local player đã lên bục xong -> báo cho camera chuyển sang cinematic
        yield return new WaitForSeconds(1f);
        OnLocalPlayerReachedPodium?.Invoke(championPos, runnerupPos, thirdplacePos);

        FinishArrange();
    }

    // Kết thúc routine ở mọi nhánh: báo cho UI tổng kết (nó tự đợi thêm vài giây rồi hiện nút "Chạm để tiếp tục").
    private void FinishArrange()
    {
        arrangeRoutine = null;
        OnArrangeCompleted?.Invoke();
    }

    private IEnumerator WaitUntilAtRest(Transform root)
    {
        float timeout = Time.time + maxWaitForRest;
        Vector3 last = root.position;
        int restCount = 0;

        while (Time.time < timeout)
        {
            yield return new WaitForFixedUpdate();

            float speed = (root.position - last).magnitude / Time.fixedDeltaTime;
            last = root.position;

            restCount = speed < restSpeedThreshold ? restCount + 1 : 0;
            if (restCount >= restFixedFrames) yield break;
        }

        Debug.LogWarning("[Podium] hết thời gian chờ mà nhân vật vẫn chưa dừng, teleport luôn");
    }

    private int GetMyRankByScore()
    {
        LeaderboardManager leaderboard = LeaderboardManager.Instance;
        if (leaderboard == null || leaderboard.Runner == null) return -1;

        PlayerRef me = leaderboard.Runner.LocalPlayer;
        var sorted = leaderboard.GetSortedScores();

        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].Player == me)
                return i;
        }
        return -1;
    }

    private NetworkObject GetLocalPlayerRoot()
    {
        Transform me = PlayerContext.Instance != null ? PlayerContext.Instance.LocalPlayer : null;
        if (me != null)
        {
            var no = me.GetComponentInParent<NetworkObject>();
            if (no != null && no.HasStateAuthority)
                return no;
        }

        foreach (var no in FindObjectsOfType<NetworkObject>())
        {
            if (!no.CompareTag("Player")) continue;
            if (no.HasStateAuthority)
                return no;
        }
        return null;
    }

    private void MoveToPodiumPosition(NetworkObject no, Transform targetPos)
    {
        if (!no.HasStateAuthority)
        {
            Debug.LogWarning("[Podium] không có StateAuthority trên nhân vật local -> Teleport sẽ bị bỏ qua");
            return;
        }

        if (!no.TryGetComponent(out NetworkTransform nt))
        {
            Debug.LogWarning("[Podium] nhân vật không có NetworkTransform");
            return;
        }

        nt.Teleport(targetPos.position, targetPos.rotation);
        Debug.Log($"[Podium] moved {no.name} to {targetPos.position}, now at {no.transform.position}");
    }
}