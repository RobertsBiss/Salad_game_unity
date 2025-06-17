using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Add TextMeshPro namespace

public class CrateController : MonoBehaviour
{
    [Header("Crate References")]
    [SerializeField] private Transform crateTop; // Assign the crate top/lid GameObject
    [SerializeField] private Transform crateCollectionZone; // Assign the collection zone child object

    [Header("Opening Settings")]
    [SerializeField] private float openAngle = 90f; // How much to rotate when opening (degrees)
    [SerializeField] private float openSpeed = 2f; // How fast the crate opens/closes
    [SerializeField] private Vector3 rotationAxis = Vector3.right; // Which axis to rotate around (X-axis by default)

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float lookAngleThreshold = 30f; // Maximum angle between player's look direction and direction to crate

    [Header("Scrap Collection Settings")]
    [SerializeField] private float scrapCheckRadius = 1f; // Radius to check for scrap items inside crate
    [SerializeField] private LayerMask scrapLayerMask = -1; // What layers to consider for scrap items
    [SerializeField] private bool showScrapCount = true; // Whether to show scrap count in console

    [Header("Selling Settings")]
    [SerializeField] private float fallbackScrapValue = 1f; // Fallback value if item doesn't have a value set
    [SerializeField] private bool showSellMessage = true; // Whether to show selling messages
    [SerializeField] private bool showIndividualItemValues = true; // Whether to show individual item values when selling
    [SerializeField] private MoneyManager moneyManager; // Reference to the MoneyManager

    [Header("UI Feedback (Optional)")]
    [SerializeField] private TextMeshProUGUI interactionPrompt; // Changed to TextMeshProUGUI
    [SerializeField] private string openPromptText = "[E] Open";
    [SerializeField] private string closePromptText = "[E] Close";
    [SerializeField] private string sellPromptText = "[E] Sell Scrap";

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip sellSound; // Sound when selling items
    [SerializeField] private AudioSource audioSource;

    [Header("Visual Effects (Optional)")]
    [SerializeField] private GameObject sellEffect; // Optional particle effect when selling

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;

    // Private variables
    private bool isOpen = false;
    private bool isAnimating = false;
    private bool playerInRange = false;
    private Vector3 closedRotation;
    private Vector3 openRotation;
    private Transform playerTransform;
    private Coroutine animationCoroutine;

    // Scrap tracking
    private List<ItemPickup> scrapItemsInCrate = new List<ItemPickup>();
    private int scrapCount = 0;

    // Selling stats (for potential future features)
    private int totalItemsSold = 0;
    private float totalMoneyEarned = 0f;

    void Start()
    {
        // Validate crate top reference
        if (crateTop == null)
        {
            Debug.LogError("CrateController: Crate top not assigned! Please assign the crate lid/top GameObject.");
            enabled = false;
            return;
        }

        // Setup collection zone if not assigned
        if (crateCollectionZone == null)
        {
            // Try to find a child object named "CollectionZone" or create one
            Transform foundZone = transform.Find("CollectionZone");
            if (foundZone != null)
            {
                crateCollectionZone = foundZone;
            }
            else
            {
                Debug.LogWarning("CrateController: Collection zone not assigned and none found. Please assign the collection zone child object.");
            }
        }

        // Find MoneyManager if not assigned
        if (moneyManager == null)
        {
            moneyManager = FindObjectOfType<MoneyManager>();
            if (moneyManager == null)
            {
                Debug.LogWarning("CrateController: MoneyManager not found! Please assign it in the inspector or make sure it exists in the scene.");
            }
        }

        // Store the initial (closed) rotation
        closedRotation = crateTop.localEulerAngles;

        // Calculate the open rotation based on the rotation axis and open angle
        openRotation = closedRotation + (rotationAxis * openAngle);

        // Setup audio source
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Hide interaction prompt initially
        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);

        // Find player
        FindPlayer();

        // Start checking for scrap items periodically
        StartCoroutine(CheckScrapItemsPeriodically());
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            // Fallback: try to find by FirstPersonController
            FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            else
            {
                Debug.LogWarning("CrateController: Could not find player!");
            }
        }
    }

    void Update()
    {
        // Check player distance
        CheckPlayerDistance();

        // Handle interaction input
        if (playerInRange && !isAnimating && Input.GetKeyDown(interactionKey))
        {
            ToggleCrate();
        }
    }

    // Coroutine to periodically check for scrap items in the crate
    IEnumerator CheckScrapItemsPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
            CheckScrapItemsInCrate();
        }
    }

    void CheckScrapItemsInCrate()
    {
        if (crateCollectionZone == null) return;

        // Clear the previous list
        scrapItemsInCrate.Clear();

        // Get all colliders within the scrap check radius around the collection zone
        Collider[] colliders = Physics.OverlapSphere(crateCollectionZone.position, scrapCheckRadius, scrapLayerMask);

        foreach (Collider col in colliders)
        {
            // Check if this collider belongs to an ItemPickup with scrap type
            ItemPickup itemPickup = col.GetComponent<ItemPickup>();
            if (itemPickup != null && itemPickup.item != null && itemPickup.item.type == ItemType.Scrap)
            {
                // Make sure the item is actually inside the crate bounds (additional safety check)
                if (IsItemInCrateBounds(col.transform.position))
                {
                    scrapItemsInCrate.Add(itemPickup);
                }
            }
        }

        // Update scrap count
        int newScrapCount = scrapItemsInCrate.Count;
        if (newScrapCount != scrapCount)
        {
            scrapCount = newScrapCount;
            OnScrapCountChanged();
        }
    }

    bool IsItemInCrateBounds(Vector3 itemPosition)
    {
        // Simple bounds check - you can make this more sophisticated based on your crate shape
        Vector3 localPos = transform.InverseTransformPoint(itemPosition);

        // Adjust these bounds based on your crate's actual dimensions
        // This is a simple box check - modify as needed for your crate shape
        return Mathf.Abs(localPos.x) <= 1f &&
               localPos.y >= -0.5f && localPos.y <= 1f &&
               Mathf.Abs(localPos.z) <= 1f;
    }

    void OnScrapCountChanged()
    {
        if (showScrapCount)
        {
            Debug.Log($"Scrap items in crate: {scrapCount}");

            // Optional: List the specific items with their potential value ranges
            if (scrapCount > 0)
            {
                string itemList = "Items: ";
                foreach (ItemPickup item in scrapItemsInCrate)
                {
                    if (item.item != null)
                    {
                        itemList += $"{item.item.name} ({item.item.GetValueRangeString()}), ";
                    }
                }
                Debug.Log(itemList.TrimEnd(',', ' '));
            }
        }

        // Update interaction prompt to show scrap count
        UpdatePromptText();
    }

    void CheckPlayerDistance()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = playerInRange;

        // Check if player is looking at the crate
        bool isLookingAtCrate = IsPlayerLookingAtCrate();

        // Player is in range only if they're close enough AND looking at the crate
        playerInRange = distanceToPlayer <= interactionRange && isLookingAtCrate;

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

    bool IsPlayerLookingAtCrate()
    {
        if (playerTransform == null) return false;

        // Get the direction from player to crate
        Vector3 directionToCrate = (transform.position - playerTransform.position).normalized;

        // Get the player's forward direction
        Vector3 playerForward = playerTransform.forward;

        // Calculate the angle between the two directions
        float angle = Vector3.Angle(playerForward, directionToCrate);

        // Player is looking at crate if the angle is less than the threshold
        return angle <= lookAngleThreshold;
    }

    void OnPlayerEnterRange()
    {
        if (interactionPrompt != null && !isAnimating)
        {
            interactionPrompt.gameObject.SetActive(true);
            // Update prompt text based on current state
            UpdatePromptText();
        }
    }

    void OnPlayerExitRange()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }

    void UpdatePromptText()
    {
        if (interactionPrompt != null)
        {
            string baseText = isOpen ? closePromptText : openPromptText;
            interactionPrompt.text = baseText;
        }
    }

    public void ToggleCrate()
    {
        if (isAnimating) return;

        if (isOpen)
        {
            // When closing the crate, sell the items if there are any
            if (scrapCount > 0)
            {
                SellScrapItems();
            }
            CloseCrate();
        }
        else
        {
            OpenCrate();
        }
    }

    /// <summary>
    /// Sells all scrap items currently in the crate collection zone
    /// </summary>
    public void SellScrapItems()
    {
        if (scrapItemsInCrate.Count == 0)
        {
            if (showSellMessage)
                Debug.Log("No scrap items to sell!");
            return;
        }

        // Calculate individual values and total
        float totalValue = 0f;
        int itemsSold = scrapItemsInCrate.Count;
        List<string> soldItemDetails = new List<string>();

        // Calculate value for each item before destroying them
        foreach (ItemPickup itemPickup in scrapItemsInCrate)
        {
            if (itemPickup != null && itemPickup.item != null)
            {
                float itemValue = GetItemValue(itemPickup.item);
                totalValue += itemValue;

                // Store details for the sell message
                if (showIndividualItemValues)
                {
                    soldItemDetails.Add($"{itemPickup.item.name} (${itemValue:F0})");
                }
                else
                {
                    soldItemDetails.Add(itemPickup.item.name);
                }
            }
        }

        // Play selling effects
        PlaySellEffects();

        // Destroy all scrap items in the crate
        foreach (ItemPickup item in scrapItemsInCrate)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        // Update statistics
        totalItemsSold += itemsSold;
        totalMoneyEarned += totalValue;

        // ADD MONEY TO PLAYER
        if (moneyManager != null)
        {
            moneyManager.AddMoney(Mathf.RoundToInt(totalValue));
        }
        else
        {
            Debug.LogWarning("CrateController: MoneyManager not found! Money was not added to player.");
        }

        // Show sell message
        if (showSellMessage)
        {
            Debug.Log($"SOLD {itemsSold} scrap items for ${totalValue:F0}!");
            if (showIndividualItemValues)
            {
                Debug.Log($"Items sold: {string.Join(", ", soldItemDetails)}");
            }
            else
            {
                Debug.Log($"Items sold: {string.Join(", ", soldItemDetails)}");
            }
            Debug.Log($"Total items sold today: {totalItemsSold}, Total money earned: ${totalMoneyEarned:F0}");
        }

        // Clear the list since we destroyed all items
        scrapItemsInCrate.Clear();
        scrapCount = 0;

        // Update prompt text
        UpdatePromptText();
    }

    /// <summary>
    /// Gets the value for a specific item, using the item's own value system
    /// </summary>
    /// <param name="item">The item to get the value for</param>
    /// <returns>The calculated value for this item</returns>
    private float GetItemValue(Item item)
    {
        if (item == null)
        {
            return fallbackScrapValue;
        }

        // Use the item's own GetValue() method, which handles random ranges
        return item.GetValue();
    }

    void PlaySellEffects()
    {
        // Play sell sound
        PlaySound(sellSound);

        // Play sell visual effect
        if (sellEffect != null && crateCollectionZone != null)
        {
            GameObject effect = Instantiate(sellEffect, crateCollectionZone.position, crateCollectionZone.rotation);
            Destroy(effect, 5f); // Clean up effect after 5 seconds
        }
    }

    public void OpenCrate()
    {
        if (isOpen || isAnimating) return;

        // Stop any existing animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateCrate(openRotation, true));
        PlaySound(openSound);
    }

    public void CloseCrate()
    {
        if (!isOpen || isAnimating) return;

        // Stop any existing animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateCrate(closedRotation, false));
        PlaySound(closeSound);
    }

    IEnumerator AnimateCrate(Vector3 targetRotation, bool opening)
    {
        isAnimating = true;

        // Hide interaction prompt during animation
        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);

        Vector3 startRotation = crateTop.localEulerAngles;
        float elapsedTime = 0f;
        float animationDuration = 1f / openSpeed;

        // Handle angle wrapping to ensure smooth rotation
        Vector3 normalizedStartRotation = NormalizeAngles(startRotation);
        Vector3 normalizedTargetRotation = NormalizeAngles(targetRotation);

        // Calculate the shortest path for each axis
        Vector3 deltaRotation = CalculateShortestRotationPath(normalizedStartRotation, normalizedTargetRotation);

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // Use smooth animation curve
            progress = Mathf.SmoothStep(0f, 1f, progress);

            // Interpolate rotation using the calculated delta
            Vector3 currentRotation = normalizedStartRotation + (deltaRotation * progress);
            crateTop.localEulerAngles = currentRotation;

            yield return null;
        }

        // Ensure final rotation is exact
        crateTop.localEulerAngles = targetRotation;

        // Update state
        isOpen = opening;
        isAnimating = false;

        // Show interaction prompt again if player is still in range
        if (playerInRange && interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(true);
            UpdatePromptText();
        }

        Debug.Log($"Crate {(isOpen ? "opened" : "closed")}");
    }

    Vector3 NormalizeAngles(Vector3 angles)
    {
        return new Vector3(
            NormalizeAngle(angles.x),
            NormalizeAngle(angles.y),
            NormalizeAngle(angles.z)
        );
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    Vector3 CalculateShortestRotationPath(Vector3 from, Vector3 to)
    {
        return new Vector3(
            CalculateShortestAnglePath(from.x, to.x),
            CalculateShortestAnglePath(from.y, to.y),
            CalculateShortestAnglePath(from.z, to.z)
        );
    }

    float CalculateShortestAnglePath(float from, float to)
    {
        float delta = to - from;

        // Normalize the delta to be within -180 to 180
        while (delta > 180f) delta -= 360f;
        while (delta < -180f) delta += 360f;

        return delta;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Public getters for other scripts to use
    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
    public int ScrapCount => scrapCount;
    public List<ItemPickup> ScrapItemsInCrate => new List<ItemPickup>(scrapItemsInCrate); // Return copy to prevent modification
    public int TotalItemsSold => totalItemsSold;
    public float TotalMoneyEarned => totalMoneyEarned;

    // Method to force set the crate state (useful for initialization)
    public void SetCrateState(bool open, bool animate = false)
    {
        if (animate)
        {
            if (open)
                OpenCrate();
            else
                CloseCrate();
        }
        else
        {
            isOpen = open;
            crateTop.localEulerAngles = open ? openRotation : closedRotation;
            UpdatePromptText();
        }
    }

    // Public method to manually refresh the scrap count (useful for debugging)
    [ContextMenu("Refresh Scrap Count")]
    public void RefreshScrapCount()
    {
        CheckScrapItemsInCrate();
    }

    // Method to manually trigger selling (useful for debugging or alternative UI)
    [ContextMenu("Sell All Scrap Items")]
    public void ForceSellScrapItems()
    {
        SellScrapItems();
    }

    // Method to get estimated total value of scrap (shows average of range for UI)
    public float GetEstimatedTotalScrapValue()
    {
        float totalValue = 0f;
        foreach (ItemPickup itemPickup in scrapItemsInCrate)
        {
            if (itemPickup != null && itemPickup.item != null)
            {
                // Use average of min and max for estimation
                float avgValue = (itemPickup.item.GetMinValue() + itemPickup.item.GetMaxValue()) / 2f;
                totalValue += avgValue;
            }
        }
        return totalValue;
    }

    // Method to get total value of scrap (for actual selling - DEPRECATED, use SellScrapItems instead)
    [System.Obsolete("Use SellScrapItems() instead, which handles individual item values properly")]
    public float GetTotalScrapValue()
    {
        return GetEstimatedTotalScrapValue();
    }

    // Method to reset selling statistics (useful for daily/weekly resets)
    public void ResetSellStats()
    {
        totalItemsSold = 0;
        totalMoneyEarned = 0f;
        Debug.Log("Selling statistics reset!");
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        if (playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        // Draw rotation axis
        Gizmos.color = Color.blue;
        Vector3 axisDirection = transform.TransformDirection(rotationAxis);
        Gizmos.DrawRay(transform.position, axisDirection * 2f);

        // Draw scrap collection zone
        if (crateCollectionZone != null)
        {
            Gizmos.color = scrapCount > 0 ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(crateCollectionZone.position, scrapCheckRadius);
        }

        // Draw crate bounds for item detection
        Gizmos.color = Color.magenta;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f, 1.5f, 2f)); // Adjust size based on your crate
        Gizmos.matrix = Matrix4x4.identity;
    }

    // Update OnDrawGizmos to show the look angle
    void OnDrawGizmos()
    {
        // Draw interaction range
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        if (playerTransform != null)
        {
            // Draw line to player
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);

            // Draw player's forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(playerTransform.position, playerTransform.forward * 2f);

            // Draw look angle threshold
            if (playerInRange)
            {
                Gizmos.color = Color.cyan;
                Vector3 directionToCrate = (transform.position - playerTransform.position).normalized;
                Gizmos.DrawRay(playerTransform.position, directionToCrate * 2f);
            }
        }

        // Draw rotation axis
        Gizmos.color = Color.blue;
        Vector3 axisDirection = transform.TransformDirection(rotationAxis);
        Gizmos.DrawRay(transform.position, axisDirection * 2f);

        // Draw scrap collection zone
        if (crateCollectionZone != null)
        {
            Gizmos.color = scrapCount > 0 ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(crateCollectionZone.position, scrapCheckRadius);
        }
    }
}