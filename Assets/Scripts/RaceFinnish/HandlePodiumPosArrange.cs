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

    private Transform[] podium;
    private Coroutine arrangeRoutine;

    private void Awake()
    {
        podium = new[] { championPos, runnerupPos, thirdplacePos };
    }

    private void OnEnable()
    {
        FinishLine.OnRaceFinish += HandleRaceFinish;
    }

    private void OnDisable()
    {
        FinishLine.OnRaceFinish -= HandleRaceFinish;

        if (arrangeRoutine != null)
        {
            StopCoroutine(arrangeRoutine);
            arrangeRoutine = null;
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
            arrangeRoutine = null;
            yield break;
        }

        NetworkObject no = GetLocalPlayerRoot();
        if (no == null)
        {
            Debug.LogWarning("[Podium] không tìm thấy nhân vật local");
            arrangeRoutine = null;
            yield break;
        }

        yield return WaitUntilAtRest(no.transform);

        MoveToPodiumPosition(no, podium[rank]);

        // Local player đã lên bục xong -> báo cho camera chuyển sang cinematic
        yield return new WaitForSeconds(1f);
        OnLocalPlayerReachedPodium?.Invoke(championPos, runnerupPos, thirdplacePos);

        arrangeRoutine = null;
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