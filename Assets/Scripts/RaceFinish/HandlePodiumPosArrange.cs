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

    [Header("Khi lên bục")]
    [Tooltip("Xoay thêm quanh trục Y so với góc xoay của vị trí bục (độ). 180 = quay nhân vật lại để mặt hướng về camera.")]
    [SerializeField] private float podiumYawOffset = 180f;

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
        // Không tạo mảng tạm: hàm này được gọi mỗi frame cho từng người chơi (tránh rác GC).
        int best = -1;
        float bestSqr = radius * radius;
        TryPickSlot(championPos, 0, position, ref best, ref bestSqr);
        TryPickSlot(runnerupPos, 1, position, ref best, ref bestSqr);
        TryPickSlot(thirdplacePos, 2, position, ref best, ref bestSqr);
        return best;
    }

    private static void TryPickSlot(Transform slot, int index, Vector3 position, ref int best, ref float bestSqr)
    {
        if (slot == null) return;
        float sqr = (slot.position - position).sqrMagnitude;
        if (sqr <= bestSqr)
        {
            bestSqr = sqr;
            best = index;
        }
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

        MoveToPodiumPosition(no, podium[rank], rank);

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

    // rank: 0 = hạng 1, 1 = hạng 2, ... (cùng chỉ số với podium[]).
    private void MoveToPodiumPosition(NetworkObject no, Transform targetPos, int rank)
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

        // 1. Tắt input điều khiển (vuốt/phím): người chơi không nhảy, trượt hay đổi làn được nữa khi đứng trên bục.
        PlayerInput input = no.GetComponentInChildren<PlayerInput>(true);
        if (input != null)
            input.enabled = false;

        // 2. Dịch chuyển lên bục và xoay thêm podiumYawOffset (180 độ) để mặt hướng về camera thay vì quay lưng.
        nt.Teleport(targetPos.position, targetPos.rotation * Quaternion.Euler(0f, podiumYawOffset, 0f));

        // 3. Animation: hạng 1 chạy Happy, hạng 2 trở xuống chạy idle. Qua PlayerNetworkState để máy khác cũng thấy.
        bool isChampion = rank == 0;

        PlayerAnimation playerAnim = no.GetComponentInChildren<PlayerAnimation>(true);
        if (playerAnim != null)
        {
            if (isChampion) playerAnim.PlayHappy();
            else playerAnim.PlayIdle();
        }

        PlayerNetworkState netState = no.GetComponentInChildren<PlayerNetworkState>(true);
        if (netState != null)
            netState.NotifyCelebrate(isChampion);

        Debug.Log($"[Podium] moved {no.name} to {targetPos.position}, now at {no.transform.position}, hạng {rank + 1}");
    }
}