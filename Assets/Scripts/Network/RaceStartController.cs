using Fusion;
using UnityEngine;
using System;

public class RaceStartController : NetworkBehaviour
{
    [SerializeField] private float countdownDuration = 8f;

    [Header("Đồng bộ giờ xuất phát")]
    [Tooltip("Bật: mỗi máy bắt đầu chạy ngay khi TickTimer hết ở máy đó, không đợi biến mạng RaceStarted (đến muộn theo ping). " +
             "CHỈ bật sau khi log [StartSync] cho thấy timer hết gần như cùng lúc ở các máy.")]
    [SerializeField] private bool startOnLocalTimerExpiry = false;
    [Tooltip("In log [StartSync] (giờ UTC lúc timer hết cục bộ / lúc RaceStarted tới) để đo độ lệch giữa các máy.")]
    [SerializeField] private bool logStartSync = true;

    /// <summary>Tổng thời gian đếm ngược (giây). 0 khi chưa có RaceStartController nào được spawn.</summary>
    public static float CountdownTotal { get; private set; }

    /// <summary>Số giây còn lại của đếm ngược trên máy này. -1 nếu chưa có dữ liệu (chưa spawn / timer chưa đồng bộ); 0 khi đã hết.</summary>
    public static float CountdownRemaining { get; private set; } = -1f;

    [Networked] private TickTimer StartTimer { get; set; }
    [Networked] private NetworkBool RaceStarted { get; set; }

    private bool _isSpawned = false;
    private bool _hasFiredStartEvent = false; // tránh phát event nhiều lần
    private bool _loggedLocalExpiry;
    private bool _loggedRaceStartedSeen;

    public static event Action OnRaceStarted;

    public override void Spawned()
    {
        _isSpawned = true;
        CountdownTotal = countdownDuration;
        CountdownRemaining = -1f;
        if (Runner.IsSharedModeMasterClient)
            StartTimer = TickTimer.CreateFromSeconds(Runner, countdownDuration);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isSpawned = false;
        CountdownTotal = 0f;
        CountdownRemaining = -1f;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsSharedModeMasterClient) return;

        if (!RaceStarted && StartTimer.Expired(Runner))
            RaceStarted = true;
    }

    void Update()
    {
        if (!_isSpawned) return;

        LogStartSync();

        float? remaining = StartTimer.RemainingTime(Runner);
        CountdownRemaining = remaining.HasValue ? Mathf.Max(0f, remaining.Value) : (RaceStarted ? 0f : -1f);

        if (remaining.HasValue && remaining.Value > 0f)
        {
            UI_CountdownText.Instance.UpdateCountdown(remaining.Value);
        }
        else if (!_hasFiredStartEvent && (RaceStarted || (startOnLocalTimerExpiry && StartTimer.Expired(Runner))))
        {
            UI_CountdownText.Instance.HideCountdown();
            _hasFiredStartEvent = true;
            OnRaceStarted?.Invoke(); // báo cho MỌI client biết race đã bắt đầu
        }
    }

    // Đo: giờ UTC lúc TickTimer hết ở máy này và lúc biến RaceStarted tới. Chạy nhiều bản trên cùng một PC thì so sánh
    // trực tiếp được giữa các bản: timer hết cùng lúc nhưng RaceStarted tới lệch nhau => tick đã đồng bộ, bật được startOnLocalTimerExpiry.
    private void LogStartSync()
    {
        if (!logStartSync) return;

        if (!_loggedLocalExpiry && StartTimer.Expired(Runner))
        {
            _loggedLocalExpiry = true;
            Debug.Log($"[StartSync] timer hết cục bộ lúc {System.DateTime.UtcNow:HH:mm:ss.fff} UTC, tick={Runner.Tick}, master={Runner.IsSharedModeMasterClient}");
        }

        if (!_loggedRaceStartedSeen && RaceStarted)
        {
            _loggedRaceStartedSeen = true;
            Debug.Log($"[StartSync] RaceStarted tới lúc {System.DateTime.UtcNow:HH:mm:ss.fff} UTC, tick={Runner.Tick}, master={Runner.IsSharedModeMasterClient}");
        }
    }
}