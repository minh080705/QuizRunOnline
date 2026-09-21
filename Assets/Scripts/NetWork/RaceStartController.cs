using Fusion;
using UnityEngine;
using System;

public class RaceStartController : NetworkBehaviour
{
    [SerializeField] private float countdownDuration = 3f;

    [Networked] private TickTimer StartTimer { get; set; }
    [Networked] private NetworkBool RaceStarted { get; set; }

    private bool _isSpawned = false;
    private bool _hasFiredStartEvent = false; // tránh phát event nhiều lần

    public static event Action OnRaceStarted;

    public override void Spawned()
    {
        _isSpawned = true;
        if (Runner.IsSharedModeMasterClient)
            StartTimer = TickTimer.CreateFromSeconds(Runner, countdownDuration);
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

        float? remaining = StartTimer.RemainingTime(Runner);

        if (remaining.HasValue && remaining.Value > 0f)
        {
            UI_CountdownText.Instance.UpdateCountdown(remaining.Value);
        }
        else if (RaceStarted && !_hasFiredStartEvent)
        {
            UI_CountdownText.Instance.HideCountdown();
            _hasFiredStartEvent = true;
            OnRaceStarted?.Invoke(); // báo cho MỌI client biết race đã bắt đầu
        }
    }
}