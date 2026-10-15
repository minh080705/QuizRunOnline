using System;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Keyboard (PC)")]
    [SerializeField] private KeyCode jumpKey = KeyCode.W;
    [SerializeField] private KeyCode slideKey = KeyCode.S;
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;

    [Header("Swipe (Mobile)")]
    [Tooltip("Khoảng cách tối thiểu (pixel) để tính là 1 cú vuốt, tránh nhận nhầm khi chỉ chạm nhẹ. Cú vuốt được nhận ngay khi ngón tay " +
             "đi đủ xa (không chờ nhấc tay): nhỏ quá dễ nhận nhầm hướng, lớn quá phải vuốt dài mới ăn. Thử 60 đến 100 trên điện thoại.")]
    [SerializeField] private float minSwipeDistance = 50f;

    public event Action OnJumpPressed;
    public event Action OnSlidePressed;
    public event Action OnMoveLeftPressed;
    public event Action OnMoveRightPressed;

    private Vector2 touchStartPos;
    private bool isTracking;

    // Tắt component (ví dụ khi nhân vật lên bục): bỏ cú vuốt đang dở, không để nó phát lệnh khi bật lại.
    private void OnDisable()
    {
        isTracking = false;
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleSwipeInput();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(jumpKey))
            OnJumpPressed?.Invoke();

        if (Input.GetKeyDown(slideKey))
            OnSlidePressed?.Invoke();

        if (Input.GetKeyDown(moveLeftKey))
            OnMoveLeftPressed?.Invoke();

        if (Input.GetKeyDown(moveRightKey))
            OnMoveRightPressed?.Invoke();
    }

    private void HandleSwipeInput()
    {
        // Có chạm thật (mobile) thì dùng Input.touches
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            ProcessSwipePhase(touch.phase, touch.position);
            return;
        }

#if UNITY_EDITOR
        // Trong Editor / khi test bằng chuột (Device Simulator dùng mouse để giả lập touch,
        // nhưng để chắc ăn khi build test tay trên PC không có touch, vẫn hỗ trợ chuột)
        if (Input.GetMouseButtonDown(0))
            ProcessSwipePhase(TouchPhase.Began, Input.mousePosition);
        else if (Input.GetMouseButtonUp(0))
            ProcessSwipePhase(TouchPhase.Ended, Input.mousePosition);
        else if (Input.GetMouseButton(0))
            ProcessSwipePhase(TouchPhase.Moved, Input.mousePosition);
#endif
    }

    private void ProcessSwipePhase(TouchPhase phase, Vector2 position)
    {
        switch (phase)
        {
            case TouchPhase.Began:
                touchStartPos = position;
                isTracking = true;
                break;

            case TouchPhase.Moved:
            case TouchPhase.Ended:
                // Nhận cú vuốt NGAY khi ngón tay đã đi đủ xa (Moved), không chờ nhấc tay (Ended). Trước đây chỉ xử lý
                // lúc Ended nên nhân vật phản ứng trễ đúng bằng thời gian vuốt (cỡ 0,1 đến 0,3 giây).
                if (isTracking) TrySwipe(position);
                if (phase == TouchPhase.Ended) isTracking = false;
                break;

            case TouchPhase.Canceled:
                isTracking = false;
                break;
        }
    }

    // Đủ khoảng cách thì phát đúng một lệnh rồi ngừng theo dõi tới lần chạm kế tiếp (mỗi lần chạm tối đa một cú vuốt).
    private void TrySwipe(Vector2 position)
    {
        Vector2 delta = position - touchStartPos;

        if (delta.magnitude < minSwipeDistance)
            return; // chưa đủ xa (hoặc chạm nhầm): chờ tiếp

        isTracking = false;

        // So sánh trục nào chiếm ưu thế để xác định hướng vuốt chính
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (delta.x > 0) OnMoveRightPressed?.Invoke();
            else OnMoveLeftPressed?.Invoke();
        }
        else
        {
            if (delta.y > 0) OnJumpPressed?.Invoke();   // vuốt lên -> nhảy
            else OnSlidePressed?.Invoke();               // vuốt xuống -> trượt
        }
    }
}