using UnityEngine;
using System.Collections;
using Fusion;
public class PlayerStats : NetworkBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed;
    public float sideSpeed = 10f;
    public float jumpForce = 5f;
    public bool isGrounded = true;

    [Header("Acceleration theo thời gian")]
    [SerializeField] private float baseSpeed = 10f;
    [SerializeField] private float accelerationRate = 0.3f;
    [SerializeField] private float maxSpeed = 300f;
    public float AccelerationRate => accelerationRate;  

    //public float normalMoveSpeed;
    //public float normalSideSpeed;
    public bool isDead = false;
    public bool canMove = false;
    private void Awake()
    {    
        //normalSideSpeed = sideSpeed;
    }
    private void Start()
    {

    }
    public override void Spawned()
    {
        RacePlayersRegistry.Instance.Register(this);
        if (Object.HasInputAuthority)
        {
            PlayerContext.Instance.RegisterLocalPlayer(transform, GetComponent<PlayerStats>());
            moveSpeed = baseSpeed;
            //normalMoveSpeed = baseSpeed;
        }
       
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        RacePlayersRegistry.Instance.Unregister(this); 
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!canMove || isDead) return;

        // Tự tăng tốc theo thời gian, cộng dồn trên nền tốc độ hiện tại (đã gồm buff/debuff)
        if (moveSpeed < maxSpeed)
        {
            moveSpeed = Mathf.Min(moveSpeed + accelerationRate * Runner.DeltaTime, maxSpeed);
        }
    }
    private void OnEnable()
    {
        RaceStartController.OnRaceStarted += HandleRaceStarted;
    }

    private void OnDisable()
    {
        RaceStartController.OnRaceStarted -= HandleRaceStarted;
    }

    private void HandleRaceStarted()
    {
        if (!Object.HasInputAuthority) return; // chỉ enable move cho player của chính mình
        canMove = true;
    }

    private bool wasLocked = false;

    private void Update()
    {
        if (!canMove && !isDead && !wasLocked)
        {
            LockMoveMent();
            wasLocked = true;
        }
        if (canMove && !isDead && wasLocked)
        {
            UnLockMoveMent();
            wasLocked = false;
        }
    }

    float moveSpeedBeforeLock;
    float sideSpeedBeforeLock;
    void LockMoveMent()
    {
        if (!isDead)
        {
            moveSpeedBeforeLock = moveSpeed;
            sideSpeedBeforeLock = sideSpeed;

            moveSpeed = 0;
            sideSpeed = 0;
        }
    }

    void UnLockMoveMent()
    {
        if (!isDead)
        {
            moveSpeed = moveSpeedBeforeLock;
            sideSpeed = sideSpeedBeforeLock;
        }
    }

    public void Dead()
    {
        moveSpeed = 0f;
        sideSpeed = 0f;
        isDead = true;
    }
}