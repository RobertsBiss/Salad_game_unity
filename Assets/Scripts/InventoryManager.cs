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

    [Header("Drop Settings")]
    public Transform dropPoint; // Where items are dropped from (usually in front of player)
    public float dropForce = 5f;
    public float dropUpwardForce = 2f;

    private int selectedSlot = -1;
    private int hotbarSize = 8; // Only first 8 slots used for hotbar navigation

    // Dictionary to store separate scales for items
    private Dictionary<Item, ItemScaleData> itemScaleData = new Dictionary<Item, ItemScaleData>();

    private void Start()
    {
        ChangeSelectedSlot(0);

        // If dropPoint is not assigned, use the itemHolder as default
        if (dropPoint == null)
            dropPoint = itemHolder;
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

        // Drop item input
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropSelectedItem();
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

                // Scale: use hand scale if available, otherwise default
                Vector3 targetScale = Vector3.one * 7f; // Default fallback
                if (itemScaleData.ContainsKey(itemInSlot.item))
                {
                    targetScale = itemScaleData[itemInSlot.item].handScale;
                }
                currentHeldItem.transform.localScale = targetScale;

                // Disable physics while in hand
                DisablePhysicsForHeldItem(currentHeldItem);
            }
        }
    }

    void DropSelectedItem()
    {
        if (selectedSlot < 0 || selectedSlot >= inventorySlots.Length)
            return;

        InventorySlot slot = inventorySlots[selectedSlot];
        InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

        if (itemInSlot != null)
        {
            Item itemToDrop = itemInSlot.item;

            // Create the dropped item in the world
            GameObject droppedItem = CreateDroppedItem(itemToDrop);

            // Remove one item from inventory
            itemInSlot.count--;
            if (itemInSlot.count <= 0)
            {
                Destroy(itemInSlot.gameObject);
            }
            else
            {
                itemInSlot.RefreshCount();
            }

            // Update the held item display
            UpdateHeldItem(forceUpdate: true);

            Debug.Log("Dropped: " + itemToDrop.name);
        }
    }

    GameObject CreateDroppedItem(Item item)
    {
        // Instantiate the item prefab in the world
        GameObject droppedItem = Instantiate(item.itemPrefab, dropPoint.position, dropPoint.rotation);

        // Set scale to world scale if available, otherwise use original scale
        if (itemScaleData.ContainsKey(item))
        {
            droppedItem.transform.localScale = itemScaleData[item].worldScale;
        }
        else
        {
            droppedItem.transform.localScale = Vector3.one; // Default fallback
        }

        // Add physics if not already present
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = droppedItem.AddComponent<Rigidbody>();
        }

        // Enable physics for the dropped item
        EnablePhysicsForDroppedItem(droppedItem);

        // Add or fix collider if needed (NON-TRIGGER)
        Collider col = droppedItem.GetComponent<Collider>();
        if (col == null)
        {
            // Try to add appropriate collider based on object
            AddBestColliderForItem(droppedItem);
        }
        else
        {
            // Make sure existing collider is properly configured
            col.isTrigger = false;
            col.enabled = true;

            // If it's a MeshCollider, ensure it's convex and not causing issues
            MeshCollider meshCol = col as MeshCollider;
            if (meshCol != null)
            {
                meshCol.convex = true;
            }
        }

        // Add ItemPickup component so it can be picked up again
        ItemPickup pickup = droppedItem.GetComponent<ItemPickup>();
        if (pickup == null)
        {
            pickup = droppedItem.AddComponent<ItemPickup>();
            pickup.inventoryManager = this;
            pickup.item = item;
            pickup.pickupRange = 3f;
            pickup.pickupKey = KeyCode.E;
        }

        // Apply drop force
        Vector3 dropDirection = dropPoint.forward + Vector3.up * 0.5f;
        rb.AddForce(dropDirection * dropForce + Vector3.up * dropUpwardForce, ForceMode.Impulse);

        return droppedItem;
    }

    // Public method for UI drag-and-drop system to drop items
    public GameObject CreateDroppedItemFromUI(Item item)
    {
        return CreateDroppedItem(item);
    }

    void DisablePhysicsForHeldItem(GameObject heldItem)
    {
        // Disable Rigidbody physics while in hand
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Disable physics
            rb.useGravity = false; // Disable gravity
        }

        // Disable colliders while in hand so they don't interfere
        Collider[] colliders = heldItem.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    void EnablePhysicsForDroppedItem(GameObject droppedItem)
    {
        // Enable Rigidbody physics when dropped
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // Enable physics
            rb.useGravity = true;   // Enable gravity
        }

        // Enable colliders when dropped
        Collider[] colliders = droppedItem.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
            // Ensure they're not triggers for proper physics
            col.isTrigger = false;
        }
    }

    // New method to add item with separate hand and world scales
    public bool AddItemWithScales(Item item, ItemScaleData scaleData)
    {
        // Store the scale data for this item type
        if (!itemScaleData.ContainsKey(item))
        {
            itemScaleData[item] = scaleData;
        }

        return AddItem(item);
    }

    // Keep the old method for backward compatibility
    public bool AddItemWithScale(Item item, Vector3 originalScale)
    {
        // Create scale data with same scale for both hand and world
        ItemScaleData scaleData = new ItemScaleData
        {
            worldScale = originalScale,
            handScale = originalScale * 7f // Apply the old scaling logic
        };

        return AddItemWithScales(item, scaleData);
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
                UpdateHeldItem(forceUpdate: true); // Update held item in case it's affected
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
                UpdateHeldItem(forceUpdate: true); // Update held item in case it's affected
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

    void AddBestColliderForItem(GameObject item)
    {
        // Get all renderers in case there are child objects
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            // Fallback: add a simple box collider
            BoxCollider boxCol = item.AddComponent<BoxCollider>();
            boxCol.isTrigger = false;
            return;
        }

        // Calculate the combined bounds of all renderers
        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            combinedBounds.Encapsulate(r.bounds);
        }

        // Convert world bounds to local space to account for scaling
        Vector3 worldSize = combinedBounds.size;
        Vector3 localSize = new Vector3(
            worldSize.x / item.transform.lossyScale.x,
            worldSize.y / item.transform.lossyScale.y,
            worldSize.z / item.transform.lossyScale.z
        );

        // Apply current scale to get the actual size we want for the collider
        Vector3 colliderSize = Vector3.Scale(localSize, item.transform.lossyScale);

        float maxDimension = Mathf.Max(colliderSize.x, colliderSize.y, colliderSize.z);
        float minDimension = Mathf.Min(colliderSize.x, colliderSize.y, colliderSize.z);

        // Determine best collider type based on shape
        if (IsSphereLike(colliderSize))
        {
            // Object is roughly spherical (like tomato)
            SphereCollider sphereCol = item.AddComponent<SphereCollider>();
            sphereCol.radius = maxDimension / 2f;
            sphereCol.isTrigger = false;
        }
        else if (IsCylinderLike(colliderSize))
        {
            // Object is roughly cylindrical (like soda can)
            CapsuleCollider capsuleCol = item.AddComponent<CapsuleCollider>();
            capsuleCol.height = colliderSize.y;
            capsuleCol.radius = Mathf.Min(colliderSize.x, colliderSize.z) / 2f;
            capsuleCol.isTrigger = false;
        }
        else
        {
            // Default to box collider for irregular shapes
            BoxCollider boxCol = item.AddComponent<BoxCollider>();
            // Set the size based on the local bounds, not the scaled bounds
            boxCol.size = localSize;
            boxCol.isTrigger = false;
        }
    }

    bool IsSphereLike(Vector3 size)
    {
        float threshold = 0.3f; // How much variation we allow for "sphere-like"
        float maxDim = Mathf.Max(size.x, size.y, size.z);
        float minDim = Mathf.Min(size.x, size.y, size.z);
        return (maxDim - minDim) / maxDim < threshold;
    }

    bool IsCylinderLike(Vector3 size)
    {
        // Check if height is significantly different from width/depth
        float heightDiff = Mathf.Abs(size.y - Mathf.Max(size.x, size.z));
        float widthDepthDiff = Mathf.Abs(size.x - size.z);
        return heightDiff > widthDepthDiff && widthDepthDiff < 0.2f;
    }

    // This method seems to be incomplete in your original code, so I'm providing a complete version
    public Item GetSelectedItem(bool use = false)
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

                // Update held item after use
                UpdateHeldItem(forceUpdate: true);
            }

            return item;
        }

        return null;
    }
}