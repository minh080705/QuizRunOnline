using Fusion;
using UnityEngine;

public class PlayerLaneSwitcher : NetworkBehaviour
{
    private enum LaneChangeDirection { None = 0, Left = 1, Right = 2 }

    [SerializeField] private PlayerInput inputHandler;
    [SerializeField] float[] laneXPositions = { -2.5f, 0f, 2.5f };
    [SerializeField] int currentLaneIndex = 1;
    [SerializeField] bool isChangingLane;
    [SerializeField] LaneChangeDirection laneChangeDirection;

    private PlayerStats stats;
    private bool _isSpawned; // cờ chặn Update() chạy trước khi Fusion sẵn sàng

    private void Start()
    {
        stats = GetComponent<PlayerStats>();
    }

    // Thay cho OnEnable - đây mới là nơi Object đã chắc chắn có giá trị hợp lệ
    public override void Spawned()
    {
        _isSpawned = true;
        currentLaneIndex = GetClosestLaneIndex(transform.position.x);
        if (Object.HasInputAuthority && inputHandler != null)
        {
            inputHandler.OnMoveLeftPressed += MoveToLeftLane;
            inputHandler.OnMoveRightPressed += MoveToRightLane;
        }
    }
    private int GetClosestLaneIndex(float currentX)
    {
        int closestIndex = 0;
        float closestDist = Mathf.Abs(laneXPositions[0] - currentX);

        for (int i = 1; i < laneXPositions.Length; i++)
        {
            float dist = Mathf.Abs(laneXPositions[i] - currentX);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }
    // Thay cho OnDisable
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (inputHandler != null)
        {
            inputHandler.OnMoveLeftPressed -= MoveToLeftLane;
            inputHandler.OnMoveRightPressed -= MoveToRightLane;
        }
    }

    void Update()
    {
        if (!_isSpawned) return; // chặn truy cập Object khi chưa sẵn sàng
        if (!Object.HasStateAuthority) return;
        if (!stats.canMove) return;

        transform.Translate(Vector3.forward * stats.moveSpeed * Time.deltaTime, Space.World);

        float targetLaneX = laneXPositions[currentLaneIndex];
        float currentZ = transform.position.z;

        if (isChangingLane && laneChangeDirection == LaneChangeDirection.Left)
        {
            transform.Translate(Vector3.left * Time.deltaTime * stats.sideSpeed, Space.World);
            if (transform.position.x <= targetLaneX)
            {
                isChangingLane = false;
                laneChangeDirection = LaneChangeDirection.None;
                transform.position = new Vector3(targetLaneX, transform.position.y, currentZ);
            }
        }
        else if (isChangingLane && laneChangeDirection == LaneChangeDirection.Right)
        {
            transform.Translate(Vector3.right * Time.deltaTime * stats.sideSpeed, Space.World);
            if (transform.position.x >= targetLaneX)
            {
                isChangingLane = false;
                laneChangeDirection = LaneChangeDirection.None;
                transform.position = new Vector3(targetLaneX, transform.position.y, currentZ);
            }
        }
    }

    public void MoveToLeftLane()
    {
        if (isChangingLane || !Object.HasStateAuthority) return;
        if (currentLaneIndex > 0)
        {
            currentLaneIndex--;
            isChangingLane = true;
            laneChangeDirection = LaneChangeDirection.Left;
        }
    }

    public void MoveToRightLane()
    {
        if (isChangingLane || !Object.HasStateAuthority) return;
        if (currentLaneIndex < laneXPositions.Length - 1)
        {
            currentLaneIndex++;
            isChangingLane = true;
            laneChangeDirection = LaneChangeDirection.Right;
        }
    }
}