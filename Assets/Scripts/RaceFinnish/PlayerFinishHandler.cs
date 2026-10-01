using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn trên Player prefab. Xử lý trình tự khi player LOCAL về đích:
/// 1. Tắt collider NGAY (không ăn thêm coin/quiz sau vạch đích)
/// 2. Cho chạy tiếp coastDuration giây
/// 3. Dừng di chuyển, ẩn nhân vật, kích hoạt chế độ spectator
///
/// KHÔNG còn là singleton: mỗi nhân vật có 1 bản, FinishLine tìm đúng bản
/// của nhân vật local qua PlayerContext.
/// </summary>
public class PlayerFinishHandler : MonoBehaviour
{
    [Header("Trình tự khi về đích")]
    [Tooltip("Thời gian (giây) cho phép player tiếp tục chạy sau khi về đích, trước khi bị ẩn đi.")]
    [SerializeField] private float coastDuration = 2f;

    [Header("Player Stats (kéo thả PlayerStats của chính GameObject này)")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Collider cần tắt NGAY khi về đích")]
    [SerializeField] private Collider[] collidersToDisableOnFinish;

    [Header("Hiển thị")]
    [Tooltip("Model/mesh của nhân vật - SetActive(false) sau coastDuration. Không tắt cả root vì root còn giữ NetworkObject/script khác.")]
    [SerializeField] private GameObject characterVisual;

  
    private bool hasFinished = false;

    /// <summary>
    /// Gọi khi player được gán thứ hạng về đích (từ FinishLine).
    /// </summary>
    public void OnFinishedRace(int rank)
    {
        if (hasFinished) return;
        hasFinished = true;

        DisableCollisions();
        StartCoroutine(FinishSequence());
    }

    private IEnumerator FinishSequence()
    {
        yield return new WaitForSeconds(coastDuration);

        StopMovement();
        //HideCharacter();
        SwitchToSpectatorCamera();
    }

    private void DisableCollisions()
    {
        if (collidersToDisableOnFinish == null) return;
        foreach (Collider col in collidersToDisableOnFinish)
        {
            if (col != null) col.enabled = false;
        }
    }

    private void StopMovement()
    {
        if (playerStats != null)
            playerStats.FinishRace();
    }

    private void HideCharacter()
    {
        if (characterVisual != null)
            characterVisual.SetActive(false);
    }
    private void MoveToStage()
    {

    }
    private void SwitchToSpectatorCamera()
    {
        // CameraFollow tự đổi followTarget khi nhận OnSpectateTargetChanged.
        RaceSpectatorManager.Instance?.ActivateSpectatorMode();
    }
}