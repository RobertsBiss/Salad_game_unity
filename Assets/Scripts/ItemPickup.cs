using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public InventoryManager inventoryManager;
    public Item item; // Changed from 'sodaCanItem' to 'item' - assign this in Inspector
    public float pickupRange = 3f;
    public float promptRange = 5f; // Distance at which the prompt becomes visible
    public KeyCode pickupKey = KeyCode.E;

    [Header("UI Feedback (Optional)")]
    public GameObject pickupPrompt; // Instance reference to the prompt
    private static GameObject staticPickupPrompt; // Static reference to the prompt, shared by all items
    public string pickupPromptText = "[E] Pick up"; // Text to show in the prompt

    [Header("Debug Settings")]
    public bool showDebugLogs = false; // Enable to see debug messages

    [Header("Hand Display Configuration")]
    [SerializeField] private bool autoConfigureFromPrefab = true; // Automatically configure the Item's hand settings based on this prefab
    [SerializeField] private bool forceReconfigure = false; // Force reconfigure even if settings exist
    [SerializeField] private Vector3 handPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 handRotationOffset = Vector3.zero;
    [SerializeField] public Vector3 handScaleMultiplier = Vector3.one;

    // Public properties to access hand display settings
    public Vector3 HandPositionOffset
    {
        get { return handPositionOffset; }
        set { handPositionOffset = value; }
    }

    public Vector3 HandRotationOffset
    {
        get { return handRotationOffset; }
        set { handRotationOffset = value; }
    }

    public Vector3 HandScaleMultiplier
    {
        get { return handScaleMultiplier; }
        set { handScaleMultiplier = value; }
    }

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
    private Camera playerCamera; // Cache player camera for raycasting

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

        // Set the tag to "Item" if not already set
        if (gameObject.tag != "Item")
        {
            gameObject.tag = "Item";
        }

        // Find inventory manager if not assigned
        if (inventoryManager == null)
            inventoryManager = FindObjectOfType<InventoryManager>();

        // Find player transform and camera for distance checking
        FindPlayer();

        // Set up the static prompt reference if this is the first item
        if (staticPickupPrompt == null && pickupPrompt != null)
        {
            staticPickupPrompt = pickupPrompt;
            staticPickupPrompt.SetActive(false);
            if (showDebugLogs)
            {
                Debug.Log($"Set up static prompt reference from {gameObject.name}");
            }
        }
    }

    void FindPlayer()
    {
        // Try to find player by tag first
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerCamera = playerObject.GetComponentInChildren<Camera>();
            return;
        }

        // Fallback: try to find by FirstPersonController component
        FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            playerCamera = playerController.GetComponentInChildren<Camera>();
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
        if (playerTransform == null || playerCamera == null)
        {
            // Try to find player again if we lost reference
            FindPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = playerInRange;
        playerInRange = distanceToPlayer <= pickupRange;

        // Cast a ray from the camera to check if we're looking at any item
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Use a layer mask that includes all layers except the player's layer
        int layerMask = ~(1 << LayerMask.NameToLayer("Player"));

        if (Physics.Raycast(ray, out hit, promptRange, layerMask))
        {
            if (showDebugLogs)
            {
                Debug.Log($"Ray hit: {hit.transform.name} at distance {hit.distance}");
                Debug.Log($"Hit object tag: {hit.transform.tag}");
            }

            // Check if the ray hit an item
            if (hit.transform.CompareTag("Item"))
            {
                // Only show prompt if we're within range of the hit item
                float hitDistance = Vector3.Distance(hit.transform.position, playerTransform.position);
                if (hitDistance <= promptRange)
                {
                    // We're looking at an item within range
                    if (staticPickupPrompt != null)
                    {
                        staticPickupPrompt.SetActive(true);
                        // Update prompt text if it has a Text component
                        UnityEngine.UI.Text promptText = staticPickupPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
                        if (promptText != null)
                        {
                            promptText.text = pickupPromptText;
                        }

                        if (showDebugLogs)
                        {
                            Debug.Log($"Showing prompt for {hit.transform.name} at distance {hitDistance}");
                        }
                    }
                    else if (showDebugLogs)
                    {
                        Debug.LogWarning("Static prompt reference is null!");
                    }
                    return;
                }
                else if (showDebugLogs)
                {
                    Debug.Log($"Item {hit.transform.name} is too far: {hitDistance} > {promptRange}");
                }
            }
            else if (showDebugLogs)
            {
                Debug.Log($"Hit object is not an item. Tag: {hit.transform.tag}");
            }
        }
        else if (showDebugLogs)
        {
            Debug.Log("No ray hit within range");
        }

        // Hide prompt if we're not looking at an item or not in range
        if (staticPickupPrompt != null)
        {
            staticPickupPrompt.SetActive(false);
            if (showDebugLogs)
            {
                Debug.Log("Hiding prompt - no item in view or out of range");
            }
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
        if (staticPickupPrompt != null)
            staticPickupPrompt.SetActive(true);
    }

    public void OnPlayerExitRange()
    {
        if (staticPickupPrompt != null)
            staticPickupPrompt.SetActive(false);
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
            // Create a custom ItemScaleData to pass both scales AND position/rotation offsets
            ItemScaleData scaleData = new ItemScaleData
            {
                worldScale = originalWorldScale,
                handScale = calculatedHandScale,
                handPositionOffset = handPositionOffset,
                handRotationOffset = handRotationOffset
            };

            // Pass both scales to the inventory manager
            bool success = inventoryManager.AddItemWithScales(item, scaleData);
            if (success)
            {
                Debug.Log("Picked up: " + item.name);
                PlayPickupEffects();

                if (staticPickupPrompt != null)
                    staticPickupPrompt.SetActive(false);

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

    // Optional: Draw pickup and prompt ranges in Scene view for debugging
    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            // Draw the ray in the scene view
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * promptRange);
        }
    }
}

// Updated data structure to hold position and rotation offsets as well
[System.Serializable]
public class ItemScaleData
{
    public Vector3 worldScale = Vector3.one;           // Scale when dropped in world
    public Vector3 handScale = Vector3.one;            // Scale when held in hand
    public Vector3 handPositionOffset = Vector3.zero;  // Position offset when held in hand
    public Vector3 handRotationOffset = Vector3.zero;  // Rotation offset when held in hand
}