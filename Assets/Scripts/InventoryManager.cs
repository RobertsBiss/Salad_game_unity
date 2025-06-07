using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public int maxStackedItems = 20;
    public InventorySlot[] inventorySlots;
    public GameObject inventoryItemPrefab;

    [Header("Held Item Display")]
    public Transform itemHolder; // Assign in Inspector
    private GameObject currentHeldItem;

    private int selectedSlot = -1;
    private int hotbarSize = 8; // Only first 8 slots used for hotbar navigation

    private void Start()
    {
        ChangeSelectedSlot(0);
    }

    private void Update()
    {
        // Number key input (1–8) for hotbar
        if (Input.inputString != null)
        {
            bool isNumber = int.TryParse(Input.inputString, out int number);
            if (isNumber && number > 0 && number <= hotbarSize)
            {
                ChangeSelectedSlot(number - 1);
            }
        }

        // Scroll wheel input (reversed direction, hotbar only)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            int newSlot = selectedSlot;

            if (scroll > 0f)
            {
                // Scroll up ? previous slot
                newSlot = (selectedSlot - 1 + hotbarSize) % hotbarSize;
            }
            else if (scroll < 0f)
            {
                // Scroll down ? next slot
                newSlot = (selectedSlot + 1) % hotbarSize;
            }

            ChangeSelectedSlot(newSlot);
        }

        // Continuously check if held item matches selected slot's item
        UpdateHeldItem();
    }

    void ChangeSelectedSlot(int newValue)
    {
        if (selectedSlot >= 0 && selectedSlot < inventorySlots.Length)
        {
            inventorySlots[selectedSlot].Deselect();
        }

        selectedSlot = newValue;

        if (selectedSlot >= 0 && selectedSlot < inventorySlots.Length)
        {
            inventorySlots[selectedSlot].Select();
        }

        UpdateHeldItem(forceUpdate: true);
    }

    void UpdateHeldItem(bool forceUpdate = false)
    {
        InventorySlot slot = inventorySlots[selectedSlot];
        InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

        // If nothing is in slot and we have a held item, destroy it
        if (itemInSlot == null && currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
            return;
        }

        // If item in slot has changed OR we force update
        if ((itemInSlot != null && (currentHeldItem == null || currentHeldItem.name != itemInSlot.item.itemPrefab.name + "(Clone)")) || forceUpdate)
        {
            if (currentHeldItem != null)
            {
                Destroy(currentHeldItem);
            }

            if (itemInSlot != null && itemInSlot.item.itemPrefab != null)
            {
                currentHeldItem = Instantiate(itemInSlot.item.itemPrefab, itemHolder);

                // Position: local to holder
                currentHeldItem.transform.localPosition = Vector3.zero;

                // Rotation: upright
                currentHeldItem.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

                // Scale: make visible and big
                currentHeldItem.transform.localScale = Vector3.one * 7f;
            }
        }
    }

    public bool AddItem(Item item)
    {
        // Try stacking to existing slot
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventorySlot slot = inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
            if (itemInSlot != null &&
                itemInSlot.item == item &&
                itemInSlot.count < maxStackedItems &&
                itemInSlot.item.stackable)
            {

                itemInSlot.count++;
                itemInSlot.RefreshCount();
                UpdateHeldItem(forceUpdate: true); // Update held item in case it’s affected
                return true;
            }
        }

        // Try placing in empty slot
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventorySlot slot = inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null)
            {
                SpawnNewItem(item, slot);
                UpdateHeldItem(forceUpdate: true); // Update held item in case it’s affected
                return true;
            }
        }

        return false;
    }

    void SpawnNewItem(Item item, InventorySlot slot)
    {
        GameObject newItemGo = Instantiate(inventoryItemPrefab, slot.transform);
        InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
        inventoryItem.InitialiseItem(item);
    }

    public Item GetSelectedItem(bool use)
    {
        if (selectedSlot < 0 || selectedSlot >= inventorySlots.Length)
            return null;

        InventorySlot slot = inventorySlots[selectedSlot];
        InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
        if (itemInSlot != null)
        {
            Item item = itemInSlot.item;
            if (use)
            {
                itemInSlot.count--;
                if (itemInSlot.count <= 0)
                {
                    Destroy(itemInSlot.gameObject);
                }
                else
                {
                    itemInSlot.RefreshCount();
                }

                // Optionally destroy held model after use
                Destroy(currentHeldItem);
                currentHeldItem = null;
            }

            return item;
        }

        return null;
    }
}
