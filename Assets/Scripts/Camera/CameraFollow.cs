using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private CameraShake cameraShake;

    [Header("Offset so với Player")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -10f);

    [Header("Rotation cố định (độ)")]
    [SerializeField] private float fixedRotationX = 8f;

    [Header("Smooth follow")]
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Podium Cinematic")]
    [SerializeField] private HandlePodiumPosArrange podium; // kéo thả Podium prefab trong scene vào đây
    [SerializeField] private float podiumPanDuration = 3f;
    [SerializeField] private Vector3 podiumStartOffset = new Vector3(0f, -1.5f, -5f); // thấp, nhìn hất lên
    [SerializeField] private Vector3 podiumEndOffset = new Vector3(0f, 3.5f, -7f);    // cao, ngang tầm mắt
    [SerializeField] private float podiumEyeHeight = 1.6f; // chiều cao mắt nhân vật, cộng vào điểm nhìn tới

    private Vector3 currentBasePos;
    private Transform followTarget;

    private bool started;
    private bool subscribedSpectator;
    private bool subscribedPodium;
    private bool inPodiumMode;
    private Coroutine podiumRoutine;

    private void Awake()
    {
        if (cameraShake == null)
            cameraShake = GetComponent<CameraShake>();

        transform.rotation = Quaternion.Euler(fixedRotationX, 0f, 0f);
    }

    private void Start()
    {
        started = true;

        SubscribeSpectator();
        SubscribePodium();

        if (followTarget == null && PlayerContext.Instance != null && PlayerContext.Instance.LocalPlayer != null)
            HandlePlayerReady(PlayerContext.Instance.LocalPlayer);
    }

    private void OnEnable()
    {
        PlayerContext.OnLocalPlayerReady += HandlePlayerReady;

        if (started)
        {
            SubscribeSpectator();
            SubscribePodium();
        }
    }

    private void OnDisable()
    {
        PlayerContext.OnLocalPlayerReady -= HandlePlayerReady;
        UnsubscribeSpectator();
        UnsubscribePodium();
    }

    private void SubscribeSpectator()
    {
        if (subscribedSpectator) return;
        if (RaceSpectatorManager.Instance == null) return;

        RaceSpectatorManager.Instance.OnSpectateTargetChanged += HandleSpectateTargetChanged;
        subscribedSpectator = true;
    }

    private void UnsubscribeSpectator()
    {
        if (!subscribedSpectator) return;
        if (RaceSpectatorManager.Instance != null)
            RaceSpectatorManager.Instance.OnSpectateTargetChanged -= HandleSpectateTargetChanged;
        subscribedSpectator = false;
    }

    private void SubscribePodium()
    {
        if (subscribedPodium) return;
        if (podium == null) return;

        podium.OnLocalPlayerReachedPodium += HandlePodiumReached;
        subscribedPodium = true;
    }

    private void UnsubscribePodium()
    {
        if (!subscribedPodium) return;
        if (podium != null)
            podium.OnLocalPlayerReachedPodium -= HandlePodiumReached;
        subscribedPodium = false;
    }

    private void HandlePlayerReady(Transform localPlayer)
    {
        if (inPodiumMode) return; // đang quay cảnh trao giải, không nhận lại follow target nữa

        followTarget = localPlayer;
        currentBasePos = followTarget.position + offset;
    }

    private void HandleSpectateTargetChanged(Transform target)
    {
        if (inPodiumMode) return;
        if (target != null)
            followTarget = target;
    }

    private void HandlePodiumReached(Transform championPos, Transform runnerupPos, Transform thirdplacePos)
    {
        inPodiumMode = true;
        followTarget = null; // ngắt hẳn chế độ follow, LateUpdate không còn chạy nhánh đó nữa

        if (podiumRoutine != null) StopCoroutine(podiumRoutine);
        podiumRoutine = StartCoroutine(PodiumPanRoutine(championPos, runnerupPos, thirdplacePos));
    }

    private IEnumerator PodiumPanRoutine(Transform championPos, Transform runnerupPos, Transform thirdplacePos)
    {
        Vector3 lookTarget = (championPos.position + runnerupPos.position + thirdplacePos.position) / 3f
                              + Vector3.up * podiumEyeHeight;

        Vector3 startPos = championPos.position + podiumStartOffset;
        Vector3 endPos = championPos.position + podiumEndOffset;

        float elapsed = 0f;
        while (elapsed < podiumPanDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / podiumPanDuration);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation((lookTarget - pos).normalized);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = Quaternion.LookRotation((lookTarget - endPos).normalized);
        podiumRoutine = null;
    }

    private void LateUpdate()
    {
        if (inPodiumMode) return; // camera đang do PodiumPanRoutine điều khiển

        if (followTarget == null) return;

        Vector3 targetPos = followTarget.position + offset;

        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        currentBasePos = Vector3.Lerp(currentBasePos, targetPos, t);

        Vector3 shake = cameraShake != null ? cameraShake.ShakeOffset : Vector3.zero;
        transform.position = currentBasePos + shake;
        transform.rotation = Quaternion.Euler(fixedRotationX, 0f, 0f);
    }
}