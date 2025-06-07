using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public InventoryManager inventoryManager;
    public Item sodaCanItem; // Assign this in Inspector
    public float pickupRange = 3f;
    public KeyCode pickupKey = KeyCode.E;

    [Header("UI Feedback (Optional)")]
    public GameObject pickupPrompt; // Optional UI element to show "Press E to pickup"

    private bool playerInRange = false;

    void Start()
    {
        // Hide pickup prompt initially
        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(pickupKey))
        {
            TryPickupItem();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (pickupPrompt != null)
                pickupPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (pickupPrompt != null)
                pickupPrompt.SetActive(false);
        }
    }

    void TryPickupItem()
    {
        if (sodaCanItem != null && inventoryManager != null)
        {
            inventoryManager.AddItem(sodaCanItem);
            Debug.Log("Picked up: " + sodaCanItem.name);
            if (pickupPrompt != null)
                pickupPrompt.SetActive(false);

            Destroy(gameObject); // Remove this object from the scene
        }
        else
        {
            Debug.LogWarning("SodaCanItem or InventoryManager not assigned!");
        }
    }
}