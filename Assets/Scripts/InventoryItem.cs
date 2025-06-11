using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    public Image image;
    public Text countText;

    [Header("Drop Settings")]
    public InventoryManager inventoryManager;

    [HideInInspector] public Item item;
    [HideInInspector] public int count = 1;
    [HideInInspector] public Transform parentAfterDrag;

    private Transform originalParent;

    void Start()
    {
        // Find inventory manager if not assigned
        if (inventoryManager == null)
            inventoryManager = FindObjectOfType<InventoryManager>();
    }

    public void InitialiseItem(Item newItem)
    {
        item = newItem;
        image.sprite = newItem.image;
        RefreshCount();
    }

    public void RefreshCount()
    {
        countText.text = count.ToString();
        bool textActive = count > 1;
        countText.gameObject.SetActive(textActive);
    }

    // Drag and drop
    public void OnBeginDrag(PointerEventData eventData)
    {
        image.raycastTarget = false;
        parentAfterDrag = transform.parent;
        originalParent = transform.parent; // Store original parent
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        image.raycastTarget = true;

        // Check if we dropped on a valid inventory slot
        if (!IsDroppedOnValidSlot(eventData))
        {
            // Drop the item in the world
            DropItemInWorld();
            return;
        }

        // Normal behavior - return to slot
        transform.SetParent(parentAfterDrag);
    }

    bool IsDroppedOnValidSlot(PointerEventData eventData)
    {
        // Check if cursor is over a valid inventory slot
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // Only consider it valid if we're directly over an InventorySlot
            InventorySlot slot = result.gameObject.GetComponent<InventorySlot>();
            if (slot != null)
            {
                // Make sure this slot can accept the item (either empty or same item type for stacking)
                InventoryItem existingItem = slot.GetComponentInChildren<InventoryItem>();
                if (existingItem == null || (existingItem.item == this.item && this.item.stackable))
                {
                    parentAfterDrag = slot.transform;
                    return true;
                }
            }
        }

        return false;
    }

    void DropItemInWorld()
    {
        if (inventoryManager != null && item != null)
        {
            // Create the dropped item in the world
            GameObject droppedItem = inventoryManager.CreateDroppedItemFromUI(item);

            // Remove one item from this stack
            count--;
            if (count <= 0)
            {
                // If no items left, destroy this UI element
                Destroy(gameObject);
            }
            else
            {
                // Update the count display and return to original slot
                RefreshCount();
                transform.SetParent(originalParent);
            }

            Debug.Log("Dropped item in world: " + item.name);
        }
        else
        {
            // Fallback - return to original position if we can't drop
            transform.SetParent(originalParent);
            Debug.LogWarning("Could not drop item - InventoryManager or Item is null");
        }
    }
}