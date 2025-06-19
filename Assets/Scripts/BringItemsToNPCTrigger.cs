using UnityEngine;
using TMPro;

public class BringItemsToNPCTrigger : MonoBehaviour
{
    [Header("Mission Settings")]
    public Mission bringToolsMission; // First mission
    public Mission bringIngredientsMission; // Second mission
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public string[] toolsItemNames = { "knife", "bowl", "fork", "cutting board" }; // For tools
    public string[] ingredientsItemNames = { "tomato", "avocado", "cabbage", "onion", "carrot", "paprika" }; // For ingredients, extend as needed

    [Header("UI Feedback (Optional)")]
    public GameObject interactionPrompt; // Assign a UI prompt if desired
    public string promptText = "[E] Give items to NPC";
    public string deliveredText = "Kitchen utensils delivered!";
    public string deliveredIngredientsText = "Ingredients delivered!";

    private bool playerInRange = false;
    private InventoryManager inventoryManager;
    private Mission currentMission;
    private string[] currentRequiredItems;
    private bool waitingForNextMission = false;
    private bool justCompletedMission = false;

    void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        // Start with the first mission
        currentMission = bringToolsMission;
        currentRequiredItems = toolsItemNames;
    }

    void Update()
    {
        // No longer need to check for interactKey
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            // Only show prompt if a mission is active and not completed
            var missionManager = MissionManager.Instance;
            bool isActive = currentMission != null && missionManager != null && missionManager.IsMissionActive(currentMission);
            bool isCompleted = currentMission != null && missionManager != null && missionManager.IsMissionCompleted(currentMission);
            if (interactionPrompt != null && isActive && !isCompleted)
                interactionPrompt.SetActive(true);
            else if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
            // Automatically try to give items when entering
            TryGiveItemsToNPC();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }

    void TryGiveItemsToNPC()
    {
        if (inventoryManager == null || currentMission == null) return;
        var missionManager = MissionManager.Instance;
        if (missionManager == null) return;

        // Only allow if mission is active and not completed
        bool isActive = missionManager.IsMissionActive(currentMission);
        bool isCompleted = missionManager.IsMissionCompleted(currentMission);
        if (!isActive || isCompleted || waitingForNextMission) return;

        // Check for all required items
        InventorySlot[] slots = inventoryManager.inventorySlots;
        bool[] hasItem = new bool[currentRequiredItems.Length];
        int[] slotIndex = new int[currentRequiredItems.Length];
        for (int i = 0; i < currentRequiredItems.Length; i++)
        {
            hasItem[i] = false;
            slotIndex[i] = -1;
            for (int s = 0; s < slots.Length; s++)
            {
                InventoryItem itemInSlot = slots[s].GetComponentInChildren<InventoryItem>();
                if (itemInSlot != null && itemInSlot.item != null && itemInSlot.item.name.ToLower().Contains(currentRequiredItems[i]))
                {
                    if (itemInSlot.count > 0)
                    {
                        hasItem[i] = true;
                        slotIndex[i] = s;
                        break;
                    }
                }
            }
        }
        // If player has all items
        bool allItems = true;
        foreach (bool b in hasItem) if (!b) allItems = false;
        if (allItems)
        {
            // Remove 1 of each item
            for (int i = 0; i < currentRequiredItems.Length; i++)
            {
                if (slotIndex[i] >= 0)
                {
                    InventoryItem itemInSlot = slots[slotIndex[i]].GetComponentInChildren<InventoryItem>();
                    if (itemInSlot != null)
                    {
                        itemInSlot.count--;
                        if (itemInSlot.count <= 0)
                            Destroy(itemInSlot.gameObject);
                        else
                            itemInSlot.RefreshCount();
                    }
                }
            }
            // Mark mission as completed
            missionManager.AddProgress(currentMission, currentMission.requiredAmount);
            Debug.Log($"[BringItemsToNPCTrigger] Player gave all required items to NPC. Mission '{currentMission.missionName}' completed!");
            // Show success prompt if assigned
            if (interactionPrompt != null)
            {
                // Support both Text and TextMeshProUGUI
                var textComp = interactionPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
                var tmpComp = interactionPrompt.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    if (currentMission == bringToolsMission)
                        textComp.text = deliveredText;
                    else if (currentMission == bringIngredientsMission)
                        textComp.text = deliveredIngredientsText;
                }
                if (tmpComp != null)
                {
                    if (currentMission == bringToolsMission)
                        tmpComp.text = deliveredText;
                    else if (currentMission == bringIngredientsMission)
                        tmpComp.text = deliveredIngredientsText;
                }
                CancelInvoke(nameof(HidePrompt));
                justCompletedMission = true;
                interactionPrompt.SetActive(true);
                Invoke(nameof(HidePrompt), 2f);
            }
            // If this was the first mission, switch to the second after 2 seconds
            if (currentMission == bringToolsMission && bringIngredientsMission != null)
            {
                waitingForNextMission = true;
                Invoke(nameof(SwitchToIngredientsMission), 2f);
            }
        }
        else
        {
            Debug.Log("[BringItemsToNPCTrigger] Player does not have all required items.");
        }
    }

    void SwitchToIngredientsMission()
    {
        currentMission = bringIngredientsMission;
        currentRequiredItems = ingredientsItemNames;
        waitingForNextMission = false;
        justCompletedMission = false; // Allow prompt for new mission
        // Optionally, show the prompt for the next mission
        if (interactionPrompt != null)
        {
            var textComp = interactionPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
            var tmpComp = interactionPrompt.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
                textComp.text = promptText;
            if (tmpComp != null)
                tmpComp.text = promptText;
            interactionPrompt.SetActive(true);
        }
        // If the player is still in range and already has all ingredients, auto-complete
        if (playerInRange)
            TryGiveItemsToNPC();
    }

    void HidePrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        // Do not reset justCompletedMission here; only reset when a new mission is set
    }
}