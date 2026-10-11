using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Kéo đúng theo thứ tự slot 1 -> slot cuối")]
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private Image[] slotIcons;

    private InventoryManager inventoryManager;

    private void OnEnable()
    {
        // Nếu local player đã sẵn sàng từ trước (UI bật sau khi player đã spawn xong)
        if (PlayerContext.Instance != null && PlayerContext.Instance.LocalInventory != null)
        {
            BindInventory(PlayerContext.Instance.LocalInventory);
        }

        PlayerContext.OnLocalPlayerReady += HandleLocalPlayerReady;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i;
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
        }
    }

    private void OnDisable()
    {
        PlayerContext.OnLocalPlayerReady -= HandleLocalPlayerReady;

        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= RefreshUI;
        }
    }

    private void HandleLocalPlayerReady(Transform playerTransform)
    {
        InventoryManager inv = playerTransform.GetComponent<InventoryManager>();
        BindInventory(inv);
    }

    private void BindInventory(InventoryManager inv)
    {
        
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= RefreshUI; // gỡ đăng ký cũ, tránh trùng lặp
        }

        inventoryManager = inv;

        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }
    }

    public void OnSlotClicked(int index)
    {
        if (inventoryManager == null) return;

        Buff item = inventoryManager.GetItemAt(index);
        if (item != null && PlayerContext.Instance?.LocalStats != null)
        {
            item.ApplyBuff(PlayerContext.Instance.LocalStats);
        }
        inventoryManager.RemoveItemAt(index);
    }

    private void RefreshUI()
    {
        if (inventoryManager == null) return;
       
        for (int i = 0; i < slotIcons.Length; i++)
        {
            Buff item = inventoryManager.GetItemAt(i);
                
          
            if (item != null && item.icon != null)
            {
                slotIcons[i].sprite = item.icon;
                slotIcons[i].enabled = true;
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;
            }
        }
    }
}