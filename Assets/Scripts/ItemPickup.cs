using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public InventoryManager inventoryManager;
    public Item item; // Changed from 'sodaCanItem' to 'item' - assign this in Inspector
    public float pickupRange = 3f;
    public KeyCode pickupKey = KeyCode.E;

    [Header("UI Feedback (Optional)")]
    public GameObject pickupPrompt; // Optional UI element to show "Press E to pickup"

    [Header("Hand Display Configuration")]
    [SerializeField] private bool autoConfigureFromPrefab = true; // Automatically configure the Item's hand settings based on this prefab
    [SerializeField] private bool forceReconfigure = false; // Force reconfigure even if settings exist
    [SerializeField] private Vector3 handPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 handRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 handScaleMultiplier = Vector3.one;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupEffect; // Optional pickup effect
    [SerializeField] private AudioClip pickupSound; // Optional pickup sound

    [Header("Distance-Based Detection")]
    [SerializeField] private LayerMask playerLayerMask = -1; // What layers to consider as "player"
    [SerializeField] private string playerTag = "Player"; // Player tag to look for

    private bool playerInRange = false;
    private Vector3 originalWorldScale; // Store the original scale for world dropping
    private Vector3 calculatedHandScale; // Store the calculated scale for hand display
    private Transform playerTransform; // Cache player transform for performance

    void Start()
    {
        // Store the original scale of this pickup item for world dropping
        originalWorldScale = transform.localScale;
        calculatedHandScale = originalWorldScale; // Default to original scale

        // Auto-configure item settings if enabled
        if (autoConfigureFromPrefab && item != null)
        {
            AutoConfigureItemSettings();
        }

        // Hide pickup prompt initially
        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);

        // Find inventory manager if not assigned
        if (inventoryManager == null)
            inventoryManager = FindObjectOfType<InventoryManager>();

        // Find player transform for distance checking
        FindPlayer();
    }

    void FindPlayer()
    {
        // Try to find player by tag first
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            return;
        }

        // Fallback: try to find by FirstPersonController component
        FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            return;
        }

        Debug.LogWarning("ItemPickup: Could not find player! Make sure player has the correct tag or FirstPersonController component.");
    }

    void Update()
    {
        // Check distance to player every frame
        CheckPlayerDistance();

        if (playerInRange && Input.GetKeyDown(pickupKey))
        {
            TryPickupItem();
        }
    }

    void CheckPlayerDistance()
    {
        if (playerTransform == null)
        {
            // Try to find player again if we lost reference
            FindPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = playerInRange;
        playerInRange = distanceToPlayer <= pickupRange;

        // Handle state changes
        if (playerInRange && !wasInRange)
        {
            OnPlayerEnterRange();
        }
        else if (!playerInRange && wasInRange)
        {
            OnPlayerExitRange();
        }
    }

    void AutoConfigureItemSettings()
    {
        if (item != null)
        {
            // Set the prefab reference if it's not already set
            if (item.itemPrefab == null)
            {
                item.itemPrefab = gameObject;
            }

            // Calculate hand display scale but DON'T modify the world scale
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Vector3 size = renderer.bounds.size;
                float maxSize = Mathf.Max(size.x, size.y, size.z);

                // Scale items to be reasonable for hand display
                float targetMaxSize = 7f;
                float scaleMultiplier = targetMaxSize / maxSize;

                // Calculate the hand scale separately from world scale
                calculatedHandScale = originalWorldScale * scaleMultiplier;
                calculatedHandScale = Vector3.Scale(calculatedHandScale, handScaleMultiplier);
            }
            else
            {
                // If no renderer, just use the hand scale multiplier with original scale
                calculatedHandScale = Vector3.Scale(originalWorldScale, handScaleMultiplier);
            }
        }
    }

    // Add this method to manually reconfigure items at runtime
    [ContextMenu("Reconfigure Item Settings")]
    public void ReconfigureItemSettings()
    {
        bool originalForceReconfigure = forceReconfigure;
        forceReconfigure = true;
        AutoConfigureItemSettings();
        forceReconfigure = originalForceReconfigure;
    }

    // These methods are called by the distance checking system
    public void OnPlayerEnterRange()
    {
        if (pickupPrompt != null)
            pickupPrompt.SetActive(true);
    }

    public void OnPlayerExitRange()
    {
        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }

    // Keep the original trigger methods for backward compatibility (optional)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerEnterRange();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerExitRange();
        }
    }

    void TryPickupItem()
    {
        if (item != null && inventoryManager != null)
        {
            // Create a custom ItemScaleData to pass both scales
            ItemScaleData scaleData = new ItemScaleData
            {
                worldScale = originalWorldScale,
                handScale = calculatedHandScale
            };

            // Pass both scales to the inventory manager
            bool success = inventoryManager.AddItemWithScales(item, scaleData);
            if (success)
            {
                Debug.Log("Picked up: " + item.name);
                PlayPickupEffects();

                if (pickupPrompt != null)
                    pickupPrompt.SetActive(false);

                Destroy(gameObject); // Remove this object from the scene
            }
            else
            {
                Debug.Log("Inventory is full!");
            }
        }
        else
        {
            Debug.LogWarning("Item or InventoryManager not assigned!");
        }
    }

    void PlayPickupEffects()
    {
        // Play pickup effect
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, transform.rotation);
            Destroy(effect, 3f); // Clean up effect after 3 seconds
        }

        // Play pickup sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
    }

    // Optional: Draw pickup range in Scene view for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);

        if (playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}

// New data structure to hold both scale types
[System.Serializable]
public class ItemScaleData
{
    public Vector3 worldScale = Vector3.one;  // Scale when dropped in world
    public Vector3 handScale = Vector3.one;   // Scale when held in hand
}