using Fusion;
using UnityEngine;

/// <summary>
/// NetworkObject đặt tại vạch đích (spawn bởi FinnishLineSpawner khi người dẫn đầu tới gần).
/// Mỗi máy tự phát hiện va chạm của ĐÚNG player do máy đó điều khiển, rồi báo cho RaceResultManager
/// (scene object luôn tồn tại) - việc xếp hạng nằm ở đó vì người chơi có thể bị loại trước khi vạch đích xuất hiện.
/// </summary>
public class FinishLine : NetworkBehaviour
{
    // Vị trí/hướng lúc spawn. Runner.Spawn(position, rotation) chỉ áp dụng trên máy đang spawn khi object
    // không có NetworkTransform; các máy khác sẽ tạo bản sao ở vị trí mặc định của prefab. Gửi qua [Networked]
    // (đặt trong onBeforeSpawned) để mọi máy đặt vạch đích đúng chỗ, cả hình ảnh lẫn collider.
    [Networked] private Vector3 SpawnPosition { get; set; }
    [Networked] private Quaternion SpawnRotation { get; set; }
    [Networked] private NetworkBool HasSpawnPose { get; set; }

    /// <summary>Gọi từ onBeforeSpawned của Runner.Spawn, trước khi object được replicate cho các máy khác.</summary>
    public void SetSpawnPose(Vector3 position, Quaternion rotation)
    {
        SpawnPosition = position;
        SpawnRotation = rotation;
        HasSpawnPose = true;
    }

    public override void Spawned()
    {
        // Chạy trên MỌI máy: đưa vạch đích về đúng vị trí đã định.
        if (HasSpawnPose)
            Object.transform.SetPositionAndRotation(SpawnPosition, SpawnRotation);

        // Podium (trong RaceScene) được thả vào FinnishLineSpawner: dời tới sát vạch đích vừa xuất hiện.
        if (FinnishLineSpawner.Instance != null)
            FinnishLineSpawner.Instance.PlacePodium(Object.transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        NetworkObject playerObject = other.GetComponent<NetworkObject>();
        if (playerObject == null)
            return;

        // Chỉ xử lý player DO CHÍNH MÁY NÀY sở hữu (mô phỏng vật lý chính xác,
        // không phải bản proxy đồng bộ qua mạng).
        if (!playerObject.HasStateAuthority)
            return;

        if (RaceResultManager.Instance == null)
        {
            Debug.LogError("[FinishLine] Không có RaceResultManager trong scene: thêm component này vào object FinishLineSpawner.");
            return;
        }

        // Hiệu ứng chúc mừng chỉ phát trên máy của chính người về đích.
        SpawnEffect.Instance?.PlayCorrectEffect(playerObject.gameObject);

        RaceResultManager.Instance.ReportFinish();
    }
}
