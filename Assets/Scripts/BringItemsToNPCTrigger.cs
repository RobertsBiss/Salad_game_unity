using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BringItemsToNPCTrigger : MonoBehaviour
{
    [Header("Mission Settings")]
    public Mission bringToolsMission; // First mission
    public Mission bringIngredientsMission; // Second mission
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public string[] toolsItemNames = { "knife", "bowl", "fork", "cutting board" }; // For tools
    public string[] ingredientsItemNames = { "tomato", "avocado", "cabbage", "onion", "carrot", "paprica" }; // For ingredients, extend as needed

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

    void Start()
    {
        // Find inventory manager if not assigned
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }
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

        // Define required amounts for ingredients
        Dictionary<string, int> requiredAmounts = new Dictionary<string, int>();
        if (currentMission == bringIngredientsMission)
        {
            requiredAmounts["tomato"] = 2;
            requiredAmounts["carrot"] = 3;
            requiredAmounts["onion"] = 2;
            requiredAmounts["cabbage"] = 1;
            requiredAmounts["avocado"] = 1;
            requiredAmounts["paprica"] = 1;
        }
        else if (currentMission == bringToolsMission)
        {
            foreach (var tool in toolsItemNames)
                requiredAmounts[tool] = 1;
        }
        else
        {
            foreach (var item in currentRequiredItems)
                requiredAmounts[item] = 1;
        }

        // Check for all required items and amounts
        InventorySlot[] slots = inventoryManager.inventorySlots;
        Dictionary<string, int> foundAmounts = new Dictionary<string, int>();
        foreach (var req in requiredAmounts)
            foundAmounts[req.Key] = 0;
        for (int s = 0; s < slots.Length; s++)
        {
            InventoryItem itemInSlot = slots[s].GetComponentInChildren<InventoryItem>();
            if (itemInSlot != null && itemInSlot.item != null)
            {
                string itemName = itemInSlot.item.name.ToLower();
                foreach (var req in requiredAmounts.Keys)
                {
                    if (itemName.Contains(req))
                    {
                        foundAmounts[req] += itemInSlot.count;
                    }
                }
            }
        }
        // If player has all required amounts
        bool allItems = true;
        foreach (var req in requiredAmounts)
        {
            if (foundAmounts[req.Key] < req.Value)
            {
                allItems = false;
                break;
            }
        }
        if (allItems)
        {
            // Remove the required amounts from inventory
            foreach (var req in requiredAmounts)
            {
                int toRemove = req.Value;
                for (int s = 0; s < slots.Length && toRemove > 0; s++)
                {
                    InventoryItem itemInSlot = slots[s].GetComponentInChildren<InventoryItem>();
                    if (itemInSlot != null && itemInSlot.item != null && itemInSlot.item.name.ToLower().Contains(req.Key))
                    {
                        int removeCount = Mathf.Min(itemInSlot.count, toRemove);
                        itemInSlot.count -= removeCount;
                        toRemove -= removeCount;
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
    }
}