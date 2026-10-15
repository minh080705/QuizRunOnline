using UnityEngine;
using System.Collections;

namespace Benjathemaker
{
    public class SimpleGemsAnim : MonoBehaviour
    {
        public enum MoveMode { Once, Loop, PingPong }

        // ===== Rotation =====
        public bool isRotating = false;
        public bool rotateX = false;
        public bool rotateY = false;
        public bool rotateZ = false;
        public float rotationSpeed = 90f; // Degrees per second

        // ===== Floating =====
        public bool isFloating = false;
        public bool useEasingForFloating = false;
        public float floatHeight = 1f;
        public float floatSpeed = 1f;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float floatTimer;

        // ===== Linear movement =====
        public bool isMoving = false;
        public bool useEasingForMoving = false;
        public MoveMode moveMode = MoveMode.PingPong;
        [Tooltip("Nếu bật, startPoint/endPoint là offset theo trục local của object (tính từ vị trí ban đầu)")]
        public bool pointsRelativeToInitial = true;
        public Vector3 startPoint = Vector3.zero;
        public Vector3 endPoint = new Vector3(3f, 0f, 0f);
        public float moveSpeed = 2f; // Units per second
        [Tooltip("Bắt đầu ở một vị trí ngẫu nhiên trên đoạn thẳng giữa 2 điểm")]
        public bool randomStartPosition = false;
        [Tooltip("Ngẫu nhiên đi về phía startPoint hoặc endPoint")]
        public bool randomDirection = false;
        private float moveTimer;
        private float moveDirection = 1f; // 1 = về phía endPoint, -1 = về phía startPoint

        // ===== Scaling =====
        private Vector3 initialScale;
        public Vector3 startScale;
        public Vector3 endScale;

        public bool isScaling = false;
        public bool useEasingForScaling = false;
        public float scaleLerpSpeed = 1f;
        private float scaleTimer;

        void Start()
        {
            initialScale = transform.localScale;
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            startScale = initialScale;
            endScale = initialScale * (endScale.magnitude / startScale.magnitude);

            InitMovement();
        }

        void InitMovement()
        {
            moveDirection = (randomDirection && Random.value < 0.5f) ? -1f : 1f;

            if (randomStartPosition)
            {
                moveTimer = Random.value;
            }
            else
            {
                // Đi ngược thì bắt đầu từ endPoint, đi xuôi thì từ startPoint
                moveTimer = moveDirection > 0 ? 0f : 1f;
            }
        }

        void Update()
        {
            // ----- Rotation -----
            if (isRotating)
            {
                Vector3 rotationVector = new Vector3(
                    rotateX ? 1 : 0,
                    rotateY ? 1 : 0,
                    rotateZ ? 1 : 0
                );
                transform.Rotate(rotationVector * rotationSpeed * Time.deltaTime);
            }

            // ----- Linear movement -----
            Vector3 basePosition = initialPosition;

            if (isMoving)
            {
                Vector3 a = GetWorldStart();
                Vector3 b = GetWorldEnd();
                float distance = Vector3.Distance(a, b);

                if (distance > 0.0001f)
                {
                    // Chia cho distance để moveSpeed là tốc độ thật (đơn vị/giây)
                    moveTimer += moveDirection * Time.deltaTime * moveSpeed / distance;

                    float t;
                    switch (moveMode)
                    {
                        case MoveMode.Once: t = Mathf.Clamp01(moveTimer); break;
                        case MoveMode.Loop: t = Mathf.Repeat(moveTimer, 1f); break;
                        default: t = Mathf.PingPong(moveTimer, 1f); break;
                    }

                    if (useEasingForMoving) t = EaseInOutQuad(t);

                    basePosition = Vector3.Lerp(a, b, t);
                }
                else
                {
                    basePosition = a;
                }
            }

            // ----- Floating -----
            Vector3 floatOffset = Vector3.zero;
            if (isFloating)
            {
                floatTimer += Time.deltaTime * floatSpeed;
                float t = Mathf.PingPong(floatTimer, 1f);
                if (useEasingForFloating) t = EaseInOutQuad(t);

                floatOffset = new Vector3(0, t * floatHeight, 0);
            }

            if (isMoving || isFloating)
            {
                transform.position = basePosition + floatOffset;
            }

            // ----- Scaling -----
            if (isScaling)
            {
                scaleTimer += Time.deltaTime * scaleLerpSpeed;
                float t = Mathf.PingPong(scaleTimer, 1f);

                if (useEasingForScaling) t = EaseInOutQuad(t);

                transform.localScale = Vector3.Lerp(startScale, endScale, t);
            }
        }

        // Chạy lại movement từ đầu (random lại vị trí/hướng nếu đang bật)
        public void ResetMovement()
        {
            InitMovement();
        }

        Vector3 GetOrigin()
        {
            // Trong Editor lúc chưa Play thì initialPosition chưa được gán
            return Application.isPlaying ? initialPosition : transform.position;
        }

        Quaternion GetOriginRotation()
        {
            // Dùng rotation lúc Start để đường đi không bị xoay theo khi bật isRotating
            return Application.isPlaying ? initialRotation : transform.rotation;
        }

        // Offset được xoay theo hướng của object: X là "bên phải" của nó, Z là "phía trước" của nó...
        Vector3 GetWorldStart() => pointsRelativeToInitial
            ? GetOrigin() + GetOriginRotation() * startPoint
            : startPoint;

        Vector3 GetWorldEnd() => pointsRelativeToInitial
            ? GetOrigin() + GetOriginRotation() * endPoint
            : endPoint;

        float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }

        // Vẽ đường đi trong Scene view để dễ chỉnh
        void OnDrawGizmosSelected()
        {
            if (!isMoving) return;
            Vector3 a = GetWorldStart();
            Vector3 b = GetWorldEnd();
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(a, 0.15f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(b, 0.15f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(a, b);
        }
    }
}