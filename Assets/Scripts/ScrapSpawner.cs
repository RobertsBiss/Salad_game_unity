using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrapSpawner : MonoBehaviour
{
    [Header("Scrap Items to Spawn")]
    [SerializeField] private Item[] scrapItems = new Item[3]; // Array for the 3 scrap items

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints; // Array of all spawn point transforms

    [Header("Spawn Settings")]
    [SerializeField] private bool spawnOnStart = true; // Whether to spawn items when the game starts
    [SerializeField] private bool preventDuplicateItems = true; // Prevent same item from spawning at multiple points
    [SerializeField] private float spawnHeight = 0.5f; // Height above spawn point to spawn items
    [SerializeField] private bool randomizeRotation = true; // Whether to randomize item rotation when spawning
    [SerializeField] private float spawnedItemPickupRange = 0.5f; // Pickup range for spawned items

    [Header("Respawn Settings")]
    [SerializeField] private bool enableRespawn = true; // Whether items should respawn after selling
    [SerializeField] private CrateController crateController; // Reference to the crate controller

    [Header("Visual Feedback")]
    [SerializeField] private GameObject spawnEffect; // Optional particle effect when spawning
    [SerializeField] private AudioClip spawnSound; // Optional sound when spawning
    [SerializeField] private AudioSource audioSource; // Audio source for spawn sounds

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showSpawnPointGizmos = true;
    [SerializeField] private Color spawnPointGizmoColor = Color.yellow;
    [SerializeField] private float gizmoSize = 0.5f;

    // Private variables for tracking spawned items
    private Dictionary<Transform, GameObject> spawnedItems = new Dictionary<Transform, GameObject>();
    private Dictionary<Transform, Item> spawnPointItems = new Dictionary<Transform, Item>();
    private HashSet<Transform> pickedUpSpawnPoints = new HashSet<Transform>(); // Track which spawn points had items picked up
    private InventoryManager inventoryManager;

    // Variables to track selling state
    private int lastKnownSoldItemsCount = 0;
    private bool wasWatchingForSale = false;

    void Start()
    {
        // Find the inventory manager
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("ScrapSpawner: InventoryManager not found! Make sure it exists in the scene.");
            return;
        }

        // Find the crate controller if not assigned
        if (crateController == null)
        {
            crateController = FindFirstObjectByType<CrateController>();
            if (crateController == null)
            {
                Debug.LogError("ScrapSpawner: CrateController not found! Please assign it in the inspector or make sure it exists in the scene.");
                return;
            }
        }

        // Setup audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Validate arrays
        if (!ValidateArrays())
        {
            return;
        }

        // Initialize the last known sold items count
        if (crateController != null)
        {
            lastKnownSoldItemsCount = crateController.TotalItemsSold;
        }

        // Spawn items on start if enabled
        if (spawnOnStart)
        {
            SpawnAllItems();
        }
    }

    void Update()
    {
        // Check if crate has just finished selling items and respawn is needed
        if (enableRespawn && pickedUpSpawnPoints.Count > 0)
        {
            CheckForCrateSelling();
        }
    }

    void CheckForCrateSelling()
    {
        if (crateController == null) return;

        // Track when player starts interacting with crate (when it has items)
        if (crateController.IsOpen && crateController.ScrapCount > 0)
        {
            wasWatchingForSale = true;
        }

        // Check if items were actually sold (sold count increased)
        int currentSoldCount = crateController.TotalItemsSold;
        if (wasWatchingForSale && currentSoldCount > lastKnownSoldItemsCount)
        {
            // Items were sold! Respawn picked up items
            if (showDebugLogs)
            {
                Debug.Log($"ScrapSpawner: Items were sold! Count increased from {lastKnownSoldItemsCount} to {currentSoldCount}");
            }

            RespawnPickedUpItems();
            lastKnownSoldItemsCount = currentSoldCount;
            wasWatchingForSale = false;
        }

        // Reset watching flag if crate is closed without selling
        if (!crateController.IsOpen && !crateController.IsAnimating && crateController.ScrapCount == 0)
        {
            wasWatchingForSale = false;
        }
    }

    void RespawnPickedUpItems()
    {
        if (pickedUpSpawnPoints.Count == 0) return;

        if (showDebugLogs)
        {
            Debug.Log($"ScrapSpawner: Respawning {pickedUpSpawnPoints.Count} items after crate selling.");
        }

        List<Transform> spawnPointsToRespawn = new List<Transform>(pickedUpSpawnPoints);
        pickedUpSpawnPoints.Clear();

        foreach (Transform spawnPoint in spawnPointsToRespawn)
        {
            if (!spawnedItems.ContainsKey(spawnPoint) || spawnedItems[spawnPoint] == null)
            {
                // Choose a random scrap item for this spawn point
                int randomIndex = Random.Range(0, scrapItems.Length);
                Item itemToRespawn = scrapItems[randomIndex];
                SpawnItemAtPoint(spawnPoint, itemToRespawn);

                // Re-enable the spawn point's collider
                Collider spawnPointCollider = spawnPoint.GetComponent<Collider>();
                if (spawnPointCollider != null)
                {
                    spawnPointCollider.enabled = true;
                }

                if (showDebugLogs)
                {
                    Debug.Log($"ScrapSpawner: Respawned '{itemToRespawn.name}' at '{spawnPoint.name}' after crate selling.");
                }
            }
        }
    }

    bool ValidateArrays()
    {
        // Check if we have scrap items
        if (scrapItems == null || scrapItems.Length == 0)
        {
            Debug.LogError("ScrapSpawner: No scrap items assigned! Please assign scrap items in the inspector.");
            return false;
        }

        // Check if we have spawn points
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("ScrapSpawner: No spawn points assigned! Please assign spawn points in the inspector.");
            return false;
        }

        // Check for null items
        for (int i = 0; i < scrapItems.Length; i++)
        {
            if (scrapItems[i] == null)
            {
                Debug.LogError($"ScrapSpawner: Scrap item at index {i} is null! Please assign all scrap items.");
                return false;
            }

            // Verify items are actually scrap type
            if (scrapItems[i].type != ItemType.Scrap)
            {
                Debug.LogWarning($"ScrapSpawner: Item '{scrapItems[i].name}' is not of type Scrap! This may cause issues with the selling system.");
            }
        }

        // Check for null spawn points
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"ScrapSpawner: Spawn point at index {i} is null! Please assign all spawn points.");
                return false;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"ScrapSpawner: Validation successful! {scrapItems.Length} scrap items, {spawnPoints.Length} spawn points.");
        }

        return true;
    }

    /// <summary>
    /// Spawns items at all spawn points
    /// </summary>
    public void SpawnAllItems()
    {
        if (!ValidateArrays()) return;

        List<Item> availableItems = new List<Item>(scrapItems);

        foreach (Transform spawnPoint in spawnPoints)
        {
            // Skip if this spawn point already has an item
            if (spawnedItems.ContainsKey(spawnPoint) && spawnedItems[spawnPoint] != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"ScrapSpawner: Spawn point '{spawnPoint.name}' already has an item, skipping.");
                }
                continue;
            }

            // Choose an item to spawn
            Item itemToSpawn = ChooseItemForSpawnPoint(spawnPoint, availableItems);

            if (itemToSpawn != null)
            {
                SpawnItemAtPoint(spawnPoint, itemToSpawn);

                // Remove from available items if preventing duplicates
                if (preventDuplicateItems)
                {
                    availableItems.Remove(itemToSpawn);

                    // If we run out of unique items, refill the list
                    if (availableItems.Count == 0)
                    {
                        availableItems = new List<Item>(scrapItems);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Chooses which item to spawn at a specific spawn point
    /// </summary>
    Item ChooseItemForSpawnPoint(Transform spawnPoint, List<Item> availableItems)
    {
        if (availableItems.Count == 0) return null;

        // For now, just pick a random item from available items
        // You could extend this to have specific items for specific spawn points
        int randomIndex = Random.Range(0, availableItems.Count);
        return availableItems[randomIndex];
    }

    /// <summary>
    /// Spawns a specific item at a specific spawn point
    /// </summary>
    void SpawnItemAtPoint(Transform spawnPoint, Item item)
    {
        if (spawnPoint == null || item == null || item.itemPrefab == null)
        {
            Debug.LogError("ScrapSpawner: Cannot spawn item - null reference!");
            return;
        }

        // Calculate spawn position (slightly above the spawn point)
        Vector3 spawnPosition = spawnPoint.position + Vector3.up * spawnHeight;

        // Calculate spawn rotation
        Quaternion spawnRotation = randomizeRotation ?
            Random.rotation :
            spawnPoint.rotation;

        // Create the item using the same method as InventoryManager's CreateDroppedItem
        GameObject spawnedItem = CreateSpawnedItem(item, spawnPosition, spawnRotation);

        if (spawnedItem != null)
        {
            // Store references
            spawnedItems[spawnPoint] = spawnedItem;
            spawnPointItems[spawnPoint] = item;

            // Disable the spawn point's collider to prevent repeated pickups
            Collider spawnPointCollider = spawnPoint.GetComponent<Collider>();
            if (spawnPointCollider != null)
            {
                spawnPointCollider.enabled = false;
            }

            // Set a fixed hand scale multiplier for the spawned item
            ItemPickup itemPickup = spawnedItem.GetComponent<ItemPickup>();
            if (itemPickup != null)
            {
                itemPickup.handScaleMultiplier = new Vector3(0.05f, 0.05f, 0.05f);
            }

            // Monitor this item for pickup if respawning is enabled
            if (enableRespawn)
            {
                StartCoroutine(MonitorItemForPickup(spawnPoint, spawnedItem));
            }

            // Play spawn effects
            PlaySpawnEffects(spawnPosition);

            if (showDebugLogs)
            {
                Debug.Log($"ScrapSpawner: Spawned '{item.name}' at '{spawnPoint.name}' with pickup range {spawnedItemPickupRange}");
            }
        }
    }

    /// <summary>
    /// Creates a spawned item identical to dropped items from the inventory system
    /// </summary>
    GameObject CreateSpawnedItem(Item item, Vector3 position, Quaternion rotation)
    {
        // Instantiate the item prefab
        GameObject spawnedItem = Instantiate(item.itemPrefab, position, rotation);

        // Set the scale to world scale (same as when dropping items)
        spawnedItem.transform.localScale = Vector3.one; // Default world scale

        // Add physics components if not already present
        Rigidbody rb = spawnedItem.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = spawnedItem.AddComponent<Rigidbody>();
        }

        // Enable physics
        rb.isKinematic = false;
        rb.useGravity = true;

        // Add or configure collider
        Collider col = spawnedItem.GetComponent<Collider>();
        if (col == null)
        {
            // Use the same collider logic as InventoryManager
            AddBestColliderForItem(spawnedItem);
        }
        else
        {
            col.isTrigger = false;
            col.enabled = true;

            // Configure mesh collider if present
            MeshCollider meshCol = col as MeshCollider;
            if (meshCol != null)
            {
                meshCol.convex = true;
            }
        }

        // Add ItemPickup component if not present
        ItemPickup pickup = spawnedItem.GetComponent<ItemPickup>();
        if (pickup == null)
        {
            pickup = spawnedItem.AddComponent<ItemPickup>();
        }

        // Configure ItemPickup component
        pickup.inventoryManager = inventoryManager;
        pickup.item = item;
        pickup.pickupRange = spawnedItemPickupRange; // Use the custom pickup range
        pickup.pickupKey = KeyCode.E;

        // Get hand display settings from the first manually placed instance of this item in the scene
        // Look for preset items that are already configured with the correct hand settings
        ItemPickup[] existingPickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
        bool foundPreset = false;

        foreach (ItemPickup existingPickup in existingPickups)
        {
            // Skip the item we just created
            if (existingPickup == pickup) continue;

            // Check if this is a preset item with the same item type
            if (existingPickup.item == item && existingPickup.gameObject != spawnedItem)
            {
                // Copy all hand display settings from the preset
                pickup.HandScaleMultiplier = existingPickup.HandScaleMultiplier;
                pickup.HandPositionOffset = existingPickup.HandPositionOffset;
                pickup.HandRotationOffset = existingPickup.HandRotationOffset;

                // Also copy the original world scale if available
                if (existingPickup.transform.localScale != Vector3.one)
                {
                    spawnedItem.transform.localScale = existingPickup.transform.localScale;
                }

                // Copy the pickup sound using reflection
                var soundField = typeof(ItemPickup).GetField("pickupSound", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (soundField != null)
                {
                    soundField.SetValue(pickup, soundField.GetValue(existingPickup));
                }

                foundPreset = true;

                if (showDebugLogs)
                {
                    Debug.Log($"ScrapSpawner: Copied hand settings from preset '{existingPickup.gameObject.name}' to spawned item");
                }
                break;
            }
        }

        // If no preset was found, try to get settings from the InventoryManager's scale data
        if (!foundPreset && inventoryManager != null)
        {
            // The InventoryManager should have the scale data for this item
            // This will be handled by the ItemPickup's AutoConfigureItemSettings method
            pickup.autoConfigureFromPrefab = true;
            pickup.forceReconfigure = true;

            if (showDebugLogs)
            {
                Debug.Log($"ScrapSpawner: No preset found for {item.name}, using auto-configuration");
            }
        }

        // Ensure the pickup component is properly configured
        // This will call AutoConfigureItemSettings if autoConfigureFromPrefab is true
        if (pickup.autoConfigureFromPrefab)
        {
            pickup.ReconfigureItemSettings();
        }

        return spawnedItem;
    }

    /// <summary>
    /// Monitors an item to detect when it's picked up (for respawning)
    /// </summary>
    IEnumerator MonitorItemForPickup(Transform spawnPoint, GameObject item)
    {
        // Wait until the item is destroyed (picked up)
        while (item != null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Item was picked up, mark this spawn point for respawning
        if (enableRespawn)
        {
            pickedUpSpawnPoints.Add(spawnPoint);

            if (showDebugLogs)
            {
                Debug.Log($"ScrapSpawner: Item at '{spawnPoint.name}' was picked up. Will respawn after next crate selling.");
            }
        }

        // Clear the spawned item reference
        spawnedItems.Remove(spawnPoint);
    }

    /// <summary>
    /// Adds the best collider for an item (copied from InventoryManager)
    /// </summary>
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

        // Convert world bounds to local space
        Vector3 worldSize = combinedBounds.size;
        Vector3 localSize = new Vector3(
            worldSize.x / item.transform.lossyScale.x,
            worldSize.y / item.transform.lossyScale.y,
            worldSize.z / item.transform.lossyScale.z
        );

        Vector3 colliderSize = Vector3.Scale(localSize, item.transform.lossyScale);

        // Determine best collider type based on shape
        if (IsSphereLike(colliderSize))
        {
            SphereCollider sphereCol = item.AddComponent<SphereCollider>();
            sphereCol.radius = Mathf.Max(colliderSize.x, colliderSize.y, colliderSize.z) / 2f;
            sphereCol.isTrigger = false;
        }
        else if (IsCylinderLike(colliderSize))
        {
            CapsuleCollider capsuleCol = item.AddComponent<CapsuleCollider>();
            capsuleCol.height = colliderSize.y;
            capsuleCol.radius = Mathf.Min(colliderSize.x, colliderSize.z) / 2f;
            capsuleCol.isTrigger = false;
        }
        else
        {
            BoxCollider boxCol = item.AddComponent<BoxCollider>();
            boxCol.size = localSize;
            boxCol.isTrigger = false;
        }
    }

    bool IsSphereLike(Vector3 size)
    {
        float threshold = 0.3f;
        float maxDim = Mathf.Max(size.x, size.y, size.z);
        float minDim = Mathf.Min(size.x, size.y, size.z);
        return (maxDim - minDim) / maxDim < threshold;
    }

    bool IsCylinderLike(Vector3 size)
    {
        float heightDiff = Mathf.Abs(size.y - Mathf.Max(size.x, size.z));
        float widthDepthDiff = Mathf.Abs(size.x - size.z);
        return heightDiff > widthDepthDiff && widthDepthDiff < 0.2f;
    }

    void PlaySpawnEffects(Vector3 position)
    {
        // Play spawn effect
        if (spawnEffect != null)
        {
            GameObject effect = Instantiate(spawnEffect, position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Play spawn sound
        if (spawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }
    }

    // Public methods for manual control

    /// <summary>
    /// Manually spawn an item at a specific spawn point
    /// </summary>
    public void SpawnItemAtIndex(int spawnPointIndex, int itemIndex)
    {
        if (spawnPointIndex < 0 || spawnPointIndex >= spawnPoints.Length)
        {
            Debug.LogError($"ScrapSpawner: Invalid spawn point index {spawnPointIndex}");
            return;
        }

        if (itemIndex < 0 || itemIndex >= scrapItems.Length)
        {
            Debug.LogError($"ScrapSpawner: Invalid item index {itemIndex}");
            return;
        }

        Transform spawnPoint = spawnPoints[spawnPointIndex];
        Item item = scrapItems[itemIndex];

        // Clear existing item if present
        ClearSpawnPoint(spawnPoint);

        // Spawn new item
        SpawnItemAtPoint(spawnPoint, item);
    }

    /// <summary>
    /// Clears a specific spawn point
    /// </summary>
    public void ClearSpawnPoint(Transform spawnPoint)
    {
        if (spawnedItems.ContainsKey(spawnPoint) && spawnedItems[spawnPoint] != null)
        {
            Destroy(spawnedItems[spawnPoint]);
            spawnedItems.Remove(spawnPoint);
        }

        // Remove from picked up spawn points if it was there
        pickedUpSpawnPoints.Remove(spawnPoint);
    }

    /// <summary>
    /// Clears all spawn points
    /// </summary>
    public void ClearAllSpawnPoints()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            ClearSpawnPoint(spawnPoint);
        }

        // Clear all picked up spawn points
        pickedUpSpawnPoints.Clear();
    }

    /// <summary>
    /// Forces respawn of all cleared spawn points
    /// </summary>
    [ContextMenu("Force Respawn All")]
    public void ForceRespawnAll()
    {
        SpawnAllItems();
    }

    /// <summary>
    /// Manually trigger respawn of picked up items (for testing)
    /// </summary>
    [ContextMenu("Force Respawn Picked Up Items")]
    public void ForceRespawnPickedUpItems()
    {
        RespawnPickedUpItems();
    }

    /// <summary>
    /// Set the pickup range for spawned items
    /// </summary>
    public void SetSpawnedItemPickupRange(float range)
    {
        spawnedItemPickupRange = range;

        // Update existing spawned items
        foreach (var spawnedItem in spawnedItems.Values)
        {
            if (spawnedItem != null)
            {
                ItemPickup pickup = spawnedItem.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    pickup.pickupRange = range;
                }
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"ScrapSpawner: Set pickup range for spawned items to {range}");
        }
    }

    // Debug and utility methods

    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        ValidateArrays();
    }

    [ContextMenu("Clear All Items")]
    public void ClearAllItems()
    {
        ClearAllSpawnPoints();
    }

    [ContextMenu("Spawn All Items")]
    public void ManualSpawnAll()
    {
        SpawnAllItems();
    }

    // Getters for other scripts
    public int GetSpawnPointCount() => spawnPoints?.Length ?? 0;
    public int GetScrapItemCount() => scrapItems?.Length ?? 0;
    public int GetActiveItemCount() => spawnedItems.Count;
    public int GetPickedUpItemCount() => pickedUpSpawnPoints.Count;
    public float GetSpawnedItemPickupRange() => spawnedItemPickupRange;

    // Gizmo drawing for visualization
    void OnDrawGizmos()
    {
        if (!showSpawnPointGizmos || spawnPoints == null) return;

        Gizmos.color = spawnPointGizmoColor;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                // Draw spawn point
                Gizmos.DrawWireSphere(spawnPoint.position, gizmoSize);

                // Draw pickup range for spawned items
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(spawnPoint.position + Vector3.up * spawnHeight, spawnedItemPickupRange);
                Gizmos.color = spawnPointGizmoColor;

                // Draw spawn height indicator
                Vector3 spawnPos = spawnPoint.position + Vector3.up * spawnHeight;
                Gizmos.DrawWireCube(spawnPos, Vector3.one * (gizmoSize * 0.5f));

                // Draw line connecting spawn point to spawn position
                Gizmos.DrawLine(spawnPoint.position, spawnPos);

                // Change color if this spawn point has an active item
                if (spawnedItems.ContainsKey(spawnPoint) && spawnedItems[spawnPoint] != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(spawnPos, gizmoSize * 0.3f);
                    Gizmos.color = spawnPointGizmoColor;
                }

                // Show picked up spawn points in red
                if (pickedUpSpawnPoints.Contains(spawnPoint))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(spawnPos, gizmoSize * 0.2f);
                    Gizmos.color = spawnPointGizmoColor;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        // Draw connections between spawn points for visualization
        Gizmos.color = Color.cyan;
        for (int i = 0; i < spawnPoints.Length - 1; i++)
        {
            if (spawnPoints[i] != null && spawnPoints[i + 1] != null)
            {
                Gizmos.DrawLine(spawnPoints[i].position, spawnPoints[i + 1].position);
            }
        }
    }

    /// <summary>
    /// Refreshes hand display settings for all spawned items
    /// Useful for debugging or when preset items are modified
    /// </summary>
    [ContextMenu("Refresh All Spawned Item Settings")]
    public void RefreshAllSpawnedItemSettings()
    {
        foreach (var kvp in spawnedItems)
        {
            if (kvp.Value != null)
            {
                ItemPickup pickup = kvp.Value.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    // Force reconfigure the settings
                    pickup.forceReconfigure = true;
                    pickup.ReconfigureItemSettings();

                    if (showDebugLogs)
                    {
                        Debug.Log($"ScrapSpawner: Refreshed settings for spawned item at {kvp.Key.name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Debug method to show which preset items are being used for hand display settings
    /// </summary>
    [ContextMenu("Debug Preset Item Usage")]
    public void DebugPresetItemUsage()
    {
        Debug.Log("=== ScrapSpawner Preset Item Debug ===");

        // Find all preset items
        ItemPickup[] allPickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
        Dictionary<Item, List<ItemPickup>> presetItems = new Dictionary<Item, List<ItemPickup>>();

        foreach (ItemPickup pickup in allPickups)
        {
            if (pickup.item != null && !spawnedItems.ContainsValue(pickup.gameObject))
            {
                // This is a preset item (not spawned)
                if (!presetItems.ContainsKey(pickup.item))
                {
                    presetItems[pickup.item] = new List<ItemPickup>();
                }
                presetItems[pickup.item].Add(pickup);
            }
        }

        // Show preset items for each scrap item type
        foreach (Item scrapItem in scrapItems)
        {
            if (scrapItem != null)
            {
                Debug.Log($"Scrap Item: {scrapItem.name}");
                if (presetItems.ContainsKey(scrapItem))
                {
                    foreach (ItemPickup preset in presetItems[scrapItem])
                    {
                        Debug.Log($"  - Preset: {preset.gameObject.name} | Scale: {preset.HandScaleMultiplier} | Pos: {preset.HandPositionOffset} | Rot: {preset.HandRotationOffset}");
                    }
                }
                else
                {
                    Debug.Log("  - No preset found, will use auto-configuration");
                }
            }
        }

        Debug.Log("=== End Debug ===");
    }
}