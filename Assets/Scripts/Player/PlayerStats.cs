    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
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

        // Tốc độ "tự nhiên" do acceleration sinh ra, không bị buff đụng vào
        private float naturalMoveSpeed;

        // Hệ số nhân tổng hợp từ tất cả buff/debuff đang active
        public float buffMultiplier = 1f;
        private readonly Dictionary<object, float> activeMultipliers = new();

        public bool isDead = false;
        public bool canMove = false;

        // Tách riêng khỏi isDead - đánh dấu player đã về đích, KHÔNG phải chết.
        // Giữ riêng để các hệ thống khác (hồi sinh, hiệu ứng chết...) không bị
        // nhầm lẫn với trạng thái hoàn thành đua.
        public bool hasFinishedRace = false;

        private void Awake()
        {
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
    
                naturalMoveSpeed = baseSpeed;
                moveSpeed = naturalMoveSpeed * buffMultiplier;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            RacePlayersRegistry.Instance.Unregister(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;
            // hasFinishedRace chặn luôn cả acceleration - thiếu guard này, player
            // sẽ tiếp tục tăng tốc ngầm (naturalMoveSpeed) dù moveSpeed đã set 0
            // 1 lần trong FinishRace(), rồi bất ngờ chạy lại nếu có gì đó khiến
            // canMove/isDead thay đổi sau đó.
            if (!canMove || isDead || hasFinishedRace) return;

            // Tự tăng tốc theo thời gian, không bị buff ghi đè trực tiếp
            if (naturalMoveSpeed < maxSpeed)
            {
                naturalMoveSpeed = Mathf.Min(naturalMoveSpeed + accelerationRate * Runner.DeltaTime, maxSpeed);
            }

            moveSpeed = naturalMoveSpeed * buffMultiplier;
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
            if (!Object.HasInputAuthority) return;
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

        private float naturalSpeedBeforeLock;
        private float sideSpeedBeforeLock;

        void LockMoveMent()
        {
            if (!isDead)
            {
                naturalSpeedBeforeLock = naturalMoveSpeed;
                sideSpeedBeforeLock = sideSpeed;

                naturalMoveSpeed = 0;
                moveSpeed = 0;
                sideSpeed = 0;
            }
        }

        void UnLockMoveMent()
        {
            if (!isDead)
            {
                naturalMoveSpeed = naturalSpeedBeforeLock;
                moveSpeed = naturalMoveSpeed * buffMultiplier;
                sideSpeed = sideSpeedBeforeLock;
            }
        }

        public void Dead()
        {
            naturalMoveSpeed = 0f;
            moveSpeed = 0f;
            sideSpeed = 0f;
            isDead = true;
        }

        /// <summary>
        /// Dừng di chuyển vĩnh viễn vì đã VỀ ĐÍCH - khác với Dead() (chết giữa
        /// đường). Tách riêng để các hệ thống khác (hồi sinh, hiệu ứng chết...)
        /// không hiểu nhầm "về đích" thành "đã chết".
        /// </summary>
        public void FinishRace()
        {
            naturalMoveSpeed = 0f;
            moveSpeed = 0f;
            sideSpeed = 0f;
            hasFinishedRace = true;
        }

        // Mỗi buff/debuff dùng chính nó (this) làm key, tránh đè lẫn nhau
        public void SetSpeedMultiplier(object source, float multiplier)
        {
            activeMultipliers[source] = multiplier;
            RecalculateBuffMultiplier();
        }

        public void RemoveSpeedMultiplier(object source)
        {
            activeMultipliers.Remove(source);
            RecalculateBuffMultiplier();
        }

        private void RecalculateBuffMultiplier()
        {
            float result = 1f;
            foreach (var m in activeMultipliers.Values) result *= m;
            buffMultiplier = result;
        }
    }