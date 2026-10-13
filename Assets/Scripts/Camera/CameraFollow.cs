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
    [Tooltip("Khi chuyển sang theo dõi người khác mà camera mới phải đi xa hơn khoảng này (mét) thì nhảy thẳng tới, " +
             "không lướt qua quãng đường chưa được dựng. Gần hơn thì vẫn lướt mượt.")]
    [SerializeField] private float spectateSnapDistance = 40f;

    [Header("Podium Cinematic")]
    [SerializeField] private HandlePodiumPosArrange podium; // kéo thả Podium prefab trong scene vào đây
    [SerializeField] private float podiumPanDuration = 3f;
    [SerializeField] private Vector3 podiumStartOffset = new Vector3(0f, -1.5f, -5f); // thấp, nhìn hất lên
    [SerializeField] private Vector3 podiumEndOffset = new Vector3(0f, 3.5f, -7f);    // cao, ngang tầm mắt
    [SerializeField] private float podiumEyeHeight = 1.6f; // chiều cao mắt nhân vật, cộng vào điểm nhìn tới

    [Header("Intro Cinematic (đếm ngược xuất phát)")]
    [SerializeField] private bool introEnabled = true;
    [Tooltip("Số giây CUỐI của đếm ngược dùng camera follow bình thường. Phần đếm ngược phía trước đó là cinematic (vd đếm 8s, giá trị 3 -> cinematic 5s).")]
    [SerializeField] private float introReturnSeconds = 3f;
    [Tooltip("Góc bắt đầu quanh nhân vật (độ): 0 = phía sau (vị trí follow), 180 = chính diện phía trước.")]
    [SerializeField] private float introStartAzimuth = 150f;
    [SerializeField] private float introStartRadius = 7f;   // khoảng cách ngang lúc bắt đầu
    [SerializeField] private float introStartHeight = 2f;   // thấp, nhìn hất lên nhân vật
    [SerializeField] private float introLookHeight = 1.2f;  // điểm nhìn tới, tính từ chân nhân vật

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
        if (target == null) return;

        followTarget = target;

        // Người được xem ở xa: nhảy thẳng tới thay vì để Lerp kéo camera bay qua vùng đường chưa được dựng.
        Vector3 desired = followTarget.position + offset;
        if ((desired - currentBasePos).sqrMagnitude > spectateSnapDistance * spectateSnapDistance)
            currentBasePos = desired;
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

    /// <summary>
    /// Tiến độ cinematic mở đầu 0..1, tính theo đồng hồ đếm ngược chung (RaceStartController) nên mọi máy,
    /// kể cả máy vào trễ, thấy cùng một cảnh. False khi chưa/không còn trong đoạn cinematic.
    /// </summary>
    private bool TryGetIntroProgress(out float t)
    {
        t = 0f;
        if (!introEnabled) return false;

        float total = RaceStartController.CountdownTotal;
        float remaining = RaceStartController.CountdownRemaining;
        float introLength = total - introReturnSeconds;

        // remaining = -1 (chưa có dữ liệu) hoặc <= introReturnSeconds (3 giây cuối / đã hết) -> camera bình thường
        if (introLength <= 0f || remaining <= introReturnSeconds) return false;

        t = Mathf.Clamp01((total - remaining) / introLength);
        return true;
    }

    // Camera bay vòng quanh nhân vật từ phía trước (thấp) về đúng vị trí + góc của camera follow.
    // Ở t = 1 vị trí = target + offset và góc = fixedRotationX nên chuyển sang follow không bị giật.
    private void ApplyIntroPose(float t)
    {
        float e = Mathf.SmoothStep(0f, 1f, t);

        Vector3 flat = new Vector3(offset.x, 0f, offset.z);
        float endRadius = flat.magnitude;
        Vector3 dir = endRadius > 0.0001f ? flat / endRadius : Vector3.back;

        float azimuth = Mathf.Lerp(introStartAzimuth, 0f, e);
        float radius = Mathf.Lerp(introStartRadius, endRadius, e);
        float height = Mathf.Lerp(introStartHeight, offset.y, e);

        Vector3 center = followTarget.position;
        Vector3 pos = center + Quaternion.AngleAxis(azimuth, Vector3.up) * (dir * radius) + Vector3.up * height;

        Quaternion lookRot = Quaternion.LookRotation(center + Vector3.up * introLookHeight - pos);
        Quaternion followRot = Quaternion.Euler(fixedRotationX, 0f, 0f);
        float rotBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, t));

        transform.SetPositionAndRotation(pos, Quaternion.Slerp(lookRot, followRot, rotBlend));
        currentBasePos = center + offset; // để lúc chuyển sang follow, camera bắt đầu đúng từ vị trí này
    }

    private void LateUpdate()
    {
        if (inPodiumMode) return; // camera đang do PodiumPanRoutine điều khiển

        if (followTarget == null) return;

        if (TryGetIntroProgress(out float introT))
        {
            ApplyIntroPose(introT);
            return;
        }

        Vector3 targetPos = followTarget.position + offset;

        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        currentBasePos = Vector3.Lerp(currentBasePos, targetPos, t);

        Vector3 shake = cameraShake != null ? cameraShake.ShakeOffset : Vector3.zero;
        transform.position = currentBasePos + shake;
        transform.rotation = Quaternion.Euler(fixedRotationX, 0f, 0f);
    }
}