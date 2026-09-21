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

    private PlayerStats playerPosition;
    private Vector3 currentBasePos; // vị trí gốc, chưa cộng shake

    private void Awake()
    {
        if (cameraShake == null)
            cameraShake = GetComponent<CameraShake>();

        // Set rotation cố định ngay từ đầu
        transform.rotation = Quaternion.Euler(fixedRotationX, 0f, 0f);
    }

    private void OnEnable()
    {
        PlayerContext.OnLocalPlayerReady += HandlePlayerReady;
    }

    private void OnDisable()
    {
        PlayerContext.OnLocalPlayerReady -= HandlePlayerReady;
    }

    private void HandlePlayerReady(Transform localPlayer)
    {
        playerPosition = PlayerContext.Instance.LocalStats;

        // Khởi tạo vị trí gốc = vị trí player + offset ngay khi player sẵn sàng
        currentBasePos = playerPosition.transform.position + offset;
    }

    private void FixedUpdate()
    {
        if (playerPosition == null) return; // chưa có player thì chưa follow

        // 1. Vị trí gốc mong muốn = vị trí player + offset cố định
        Vector3 targetPos = playerPosition.transform.position + offset;

        // 2. Lerp mượt tới vị trí gốc
        currentBasePos = Vector3.Lerp(currentBasePos, targetPos, smoothSpeed * Time.fixedDeltaTime);

        // 3. Cộng thêm shake offset (nếu có)
        transform.position = currentBasePos + cameraShake.ShakeOffset;

        // 4. Rotation luôn cố định, không đổi theo Fox
        transform.rotation = Quaternion.Euler(fixedRotationX, 0f, 0f);
    }
}