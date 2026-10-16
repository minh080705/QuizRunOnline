using Fusion;
using UnityEngine;

/// <summary>
/// Bù độ trễ HIỂN THỊ cho nhân vật của người chơi khác (proxy): đẩy root của proxy về phía trước (+Z) một đoạn
/// bằng Speed * lead, để mắt người chơi thấy nó gần với vị trí thật hơn.
///
/// lead = clamp(baseLead + latencyScale * (RTT_mình + RTT_họ) / 2, 0, maxLead)
/// - RTT_mình: NetworkRunner.GetPlayerRtt(LocalPlayer). Shared Mode chỉ đọc được RTT của chính máy mình.
/// - RTT_họ: PlayerNetworkState.PingMs, do máy của họ tự đăng lên biến mạng.
///
/// Chỉ đổi transform của proxy sau khi NetworkTransform đã ghi vị trí nội suy trong frame này (NetworkTransform chạy
/// trước LateUpdate), và chạy sớm hơn CameraFollow / name tag (order 0) để mọi nơi đọc root.position thấy cùng một giá trị.
/// Không ảnh hưởng gameplay: va chạm, nhặt đồ, vạch đích đều chỉ do máy chủ nhân vật xử lý.
///
/// Được PlayerNetworkState.Spawned() gắn vào runtime cho proxy, nên không phải sửa prefab.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ProxyLagCompensator : MonoBehaviour
{
    private const float RttSampleInterval = 0.5f;
    private const float MinSpeed = 0.01f;

    private PlayerNetworkState state;
    private float latencyScale;
    private float baseLead;
    private float maxLead;
    private float smoothTime;
    private bool debugLog;

    private float myRttSeconds;
    private float nextRttSampleTime;

    private float offset;
    private float offsetVelocity;

    // Phòng khi NetworkTransform không ghi lại vị trí ở frame này: dựa vào vị trí gốc đã lưu để offset không cộng dồn.
    private bool hasApplied;
    private Vector3 lastBasePos;
    private Vector3 lastAppliedPos;

    private float nextDebugTime;

    public void Init(PlayerNetworkState networkState, float latencyScale, float baseLead, float maxLead,
                     float smoothTime, bool debugLog)
    {
        state = networkState;
        this.latencyScale = latencyScale;
        this.baseLead = baseLead;
        this.maxLead = maxLead;
        this.smoothTime = Mathf.Max(0.001f, smoothTime);
        this.debugLog = debugLog;
    }

    private void OnDisable()
    {
        offset = 0f;
        offsetVelocity = 0f;
        hasApplied = false;
    }

    private void LateUpdate()
    {
        if (state == null || state.Object == null || !state.Object.IsValid) return;

        NetworkRunner runner = state.Runner;
        if (runner == null) return;

        if (Time.unscaledTime >= nextRttSampleTime)
        {
            nextRttSampleTime = Time.unscaledTime + RttSampleInterval;
            myRttSeconds = (float)runner.GetPlayerRtt(runner.LocalPlayer);
        }

        Vector3 pos = transform.position;
        Vector3 basePos = hasApplied && (pos - lastAppliedPos).sqrMagnitude < 1e-10f ? lastBasePos : pos;

        // Chết / đóng băng / về đích / chưa xuất phát: Speed = 0 nên offset tự co về 0 (không phá kiểm tra bục).
        float speed = state.IsDead ? 0f : state.Speed;
        float lead = 0f;
        float target = 0f;
        float theirRttSeconds = state.PingMs / 1000f;
        if (speed > MinSpeed)
        {
            lead = Mathf.Clamp(baseLead + latencyScale * (myRttSeconds + theirRttSeconds) * 0.5f, 0f, maxLead);
            target = speed * lead;
        }

        offset = Mathf.SmoothDamp(offset, target, ref offsetVelocity, smoothTime);
        if (offset < 0.001f && target <= 0f) offset = 0f;

        Vector3 applied = basePos + new Vector3(0f, 0f, offset);
        if (offset > 0f) transform.position = applied;

        lastBasePos = basePos;
        lastAppliedPos = offset > 0f ? applied : basePos;
        hasApplied = true;

        if (debugLog && Time.unscaledTime >= nextDebugTime)
        {
            nextDebugTime = Time.unscaledTime + 1f;
            Debug.Log($"[ProxyLead] {name}: myRtt={myRttSeconds * 1000f:F0}ms theirRtt={theirRttSeconds * 1000f:F0}ms " +
                      $"lead={lead * 1000f:F0}ms speed={speed:F1} offsetZ={offset:F2}m " +
                      $"(Local-Remote render time={runner.LocalRenderTime - runner.RemoteRenderTime:F3}s)");
        }
    }
}
