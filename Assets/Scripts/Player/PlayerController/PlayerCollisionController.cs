using UnityEngine;

/// Quản lý CapsuleCollider của player. Jump và Slide

public class PlayerCollisionController : MonoBehaviour
{
    [SerializeField] private CapsuleCollider col;

    [Header("Normal State (Running)")]
    [SerializeField] private Vector3 normalCenter = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private int normalDirection = 1; // 1 = Y-Axis

    [Header("Slide State")]
    [SerializeField] private Vector3 slideCenter = new Vector3(0f, 0.2f, 0f);
    [SerializeField] private int slideDirection = 2; // 2 = Z-Axis

    [Header("Jump State")]
    [SerializeField] private Vector3 jumpCenter = new Vector3(0f, 1f, 0f);
    [SerializeField] private int jumpDirection = 1; // 1 = Y-Axis

    public void SetNormal()
    {
        col.center = normalCenter;
        col.direction = normalDirection;
    }

    public void SetSlide()
    {
        col.center = slideCenter;
        col.direction = slideDirection;
    }

    public void SetJump()
    {
        col.center = jumpCenter;
        col.direction = jumpDirection;
    }
}