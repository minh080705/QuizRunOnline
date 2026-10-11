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
        public float BaseSpeed => baseSpeed;
        public float MaxSpeed => maxSpeed;

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

        // Đang dừng hẳn để trả lời câu hỏi. Tách riêng khỏi canMove: đổi canMove sẽ kích hoạt
        // LockMoveMent() trong Update và ghi đè mất tốc độ đã lưu.
        public bool isFrozenByQuestion { get; private set; }
        private float naturalSpeedBeforeQuestion;

        // Hết mạng, không hồi sinh nữa. Đồng bộ qua mạng để máy khác (và bộ sinh vật cản chạy ở máy Master)
        // biết người này đã ra khỏi cuộc đua. Chỉ chủ sở hữu được ghi (PlayerLives).
        [Networked] public NetworkBool IsEliminated { get; set; }

        // Tốc độ tự nhiên lúc chết, để hồi sinh chạy lại với tốc độ cũ; và sideSpeed gốc (chưa buff).
        private float naturalSpeedBeforeDeath;
        private float initialSideSpeed;

        private void Awake()
        {
            initialSideSpeed = sideSpeed;
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
            if (!canMove || isDead || hasFinishedRace || isFrozenByQuestion) return;

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

        /// <summary>
        /// Dừng hẳn nhân vật khi hiện câu hỏi. Lưu tốc độ tự nhiên (chưa nhân buff) để khôi phục
        /// sau khi trả lời; trong lúc dừng không tăng tốc và không đổi làn.
        /// </summary>
        public void FreezeForQuestion()
        {
            if (isFrozenByQuestion || isDead || hasFinishedRace || !canMove) return;

            isFrozenByQuestion = true;
            naturalSpeedBeforeQuestion = naturalMoveSpeed;
            naturalMoveSpeed = 0f;
            moveSpeed = 0f;
        }

        /// <summary>
        /// Chạy lại với tốc độ cũ đã lưu. Gọi TRƯỚC khi áp buff/debuff của câu trả lời,
        /// vì buff snapshot sideSpeed lúc áp dụng.
        /// </summary>
        public void UnfreezeAfterQuestion()
        {
            if (!isFrozenByQuestion) return;
            isFrozenByQuestion = false;

            // Đã chết hoặc về đích trong lúc đang hỏi thì giữ nguyên trạng thái dừng.
            if (isDead || hasFinishedRace) return;

            naturalMoveSpeed = naturalSpeedBeforeQuestion;
            moveSpeed = naturalMoveSpeed * buffMultiplier;
        }

        public void Dead()
        {
            // Lưu tốc độ để hồi sinh khôi phục. Chỉ lưu lần đầu (gọi lặp lúc đã chết thì naturalMoveSpeed đã = 0).
            // Đang bị dừng vì câu hỏi thì tốc độ thật nằm ở naturalSpeedBeforeQuestion.
            if (!isDead)
                naturalSpeedBeforeDeath = isFrozenByQuestion ? naturalSpeedBeforeQuestion : naturalMoveSpeed;

            naturalMoveSpeed = 0f;
            moveSpeed = 0f;
            sideSpeed = 0f;
            isDead = true;
        }

        /// <summary>
        /// Hồi sinh sau khi chết: chạy lại với tốc độ trước lúc chết (nhân speedFactor, tối thiểu baseSpeed).
        /// sideSpeed lấy lại từ giá trị gốc nhân hệ số buff hiện tại, không dùng giá trị lúc chết
        /// vì buff có thể đã hết hạn trong lúc chờ hồi sinh.
        /// </summary>
        public void Revive(float speedFactor = 1f)
        {
            if (!isDead || hasFinishedRace) return;

            isDead = false;
            naturalMoveSpeed = Mathf.Max(baseSpeed, naturalSpeedBeforeDeath * speedFactor);
            moveSpeed = naturalMoveSpeed * buffMultiplier;
            sideSpeed = initialSideSpeed * buffMultiplier;
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