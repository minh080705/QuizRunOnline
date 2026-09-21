using System;
using Fusion;
using UnityEngine;

public class InventoryManager : NetworkBehaviour
{
    [Tooltip("Số ô tối đa trong túi, không đổi trong lúc chơi")]
    [SerializeField] private int maxSlots = 3;

    private Buff[] slots;

    public event Action OnInventoryChanged;
    public event Action<Buff> OnItemDiscarded;

    private void Awake()
    {
        slots = new Buff[maxSlots];
    }

    public int SlotCount => maxSlots;

    public Buff GetItemAt(int index)
    {
        if (index < 0 || index >= maxSlots) return null;
        return slots[index];
    }

    public void AddItem(Buff newItem)
    {
        if (!Object.HasStateAuthority) return;
        if (newItem == null) return;

        Debug.Log($"[InventoryManager] AddItem gọi trên instance ID={GetInstanceID()}, item={newItem.name}, HasStateAuthority={Object.HasStateAuthority}");

        Buff overflowItem = slots[maxSlots - 1];

        for (int i = maxSlots - 1; i > 0; i--)
        {
            slots[i] = slots[i - 1];
        }

        slots[0] = newItem;

        string slotsLog = string.Join(", ", System.Array.ConvertAll(slots, s => s != null ? s.name : "null"));
        Debug.Log($"[InventoryManager] Sau khi add, slots hiện tại: [{slotsLog}]");

        if (overflowItem != null)
        {
            OnItemDiscarded?.Invoke(overflowItem);
        }

        OnInventoryChanged?.Invoke();
    }

    public void RemoveItemAt(int index)
    {
        if (!Object.HasStateAuthority) return; // chỉ chủ nhân vật mới được xóa đồ của chính mình
        if (index < 0 || index >= maxSlots) return;
        if (slots[index] == null) return;

        for (int i = index; i < maxSlots - 1; i++)
        {
            slots[i] = slots[i + 1];
        }

        slots[maxSlots - 1] = null;

        OnInventoryChanged?.Invoke();
    }

    public bool IsFull()
    {
        return slots[maxSlots - 1] != null;
    }
}