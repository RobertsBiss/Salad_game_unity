using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    public List<Mission> allMissions; // Assign your missions in the inspector
    public GameObject missionItemContainerPrefab; // Assign your prefab here
    public Transform missionListContent; // Assign the Content object under your ScrollView

    [Header("Active Mission Display")]
    public ActiveMissionDisplay activeMissionDisplay; // Assign the ActiveMissionDisplay component

    // For demo: track progress in a dictionary (replace with your own save/progress system)
    public Dictionary<Mission, int> missionProgress = new Dictionary<Mission, int>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        PopulateMissionList();

        // Find ActiveMissionDisplay if not assigned
        if (activeMissionDisplay == null)
        {
            activeMissionDisplay = FindObjectOfType<ActiveMissionDisplay>();
        }
    }

    public void PopulateMissionList()
    {
        UpdateCollectMissionsFromInventory();
        UpdateEarnMoneyMission();
        UpdateBuyItemMissions();
        // Clear old items
        foreach (Transform child in missionListContent)
            Destroy(child.gameObject);

        foreach (var mission in allMissions)
        {
            // Only show missions that are active (prerequisite is null or completed) or already completed
            bool isActive = IsMissionActive(mission);
            bool isCompleted = IsMissionCompleted(mission);
            if (!isActive && !isCompleted)
                continue;

            GameObject go = Instantiate(missionItemContainerPrefab, missionListContent);
            MissionItemUI ui = go.GetComponent<MissionItemUI>();
            if (ui != null)
            {
                int progress = missionProgress.ContainsKey(mission) ? missionProgress[mission] : 0;
                ui.Setup(mission, progress, isActive, isCompleted);
            }
        }

        // Notify active mission display to update
        NotifyActiveMissionDisplay();
    }

    // Call this when player makes progress on a mission
    public void AddProgress(Mission mission, int amount)
    {
        if (!missionProgress.ContainsKey(mission))
            missionProgress[mission] = 0;
        missionProgress[mission] += amount;
        PopulateMissionList();
    }

    // Call this when an item is collected (e.g., picked up)
    public void OnItemCollected(Item item)
    {
        UpdateCollectMissionsFromInventory();
        foreach (var mission in allMissions)
        {
            if (IsMissionActive(mission) && !IsMissionCompleted(mission))
            {
                // Example: Collect scrap mission
                if (mission.missionName.ToLower().Contains("collect scrap") && item.type == ItemType.Scrap)
                {
                    // No need to AddProgress here, inventory-based update is used
                }
                // Add more item collection mission types here as needed
            }
        }

        // Notify active mission display to update
        NotifyActiveMissionDisplay();
    }

    // Call this when an item is sold (e.g., via crate)
    public void OnItemSold(Item item, int amount)
    {
        foreach (var mission in allMissions)
        {
            if (IsMissionActive(mission) && !IsMissionCompleted(mission))
            {
                // Example: Sell scrap mission
                if (mission.missionName.ToLower().Contains("sell scrap") && item.type == ItemType.Scrap)
                {
                    AddProgress(mission, amount);
                }
                // Earn money mission: update after selling
                if (mission.missionName.ToLower().Contains("earn money"))
                {
                    UpdateEarnMoneyMission();
                }
            }
        }

        // Notify active mission display to update
        NotifyActiveMissionDisplay();
    }

    public bool IsMissionActive(Mission mission)
    {
        if (mission.prerequisites == null || mission.prerequisites.Length == 0)
            return true;
        foreach (var prereq in mission.prerequisites)
        {
            if (prereq == null) continue;
            if (!missionProgress.ContainsKey(prereq) || missionProgress[prereq] < prereq.requiredAmount)
                return false;
        }
        return true;
    }

    public bool IsMissionCompleted(Mission mission)
    {
        return missionProgress.ContainsKey(mission) && missionProgress[mission] >= mission.requiredAmount;
    }

    public void UpdateCollectMissionsFromInventory()
    {
        var inventory = FindObjectOfType<InventoryManager>();
        if (inventory == null)
        {
            Debug.LogWarning("[MissionManager] InventoryManager not found in scene.");
            return;
        }
        foreach (var mission in allMissions)
        {
            if (mission.missionName.ToLower().Contains("collect scrap"))
            {
                // Only update progress if not already completed
                if (!IsMissionCompleted(mission))
                {
                    int scrapCount = inventory.GetItemCountByType(ItemType.Scrap);
                    Debug.Log($"[MissionManager] Updating mission '{mission.missionName}' with scrap count: {scrapCount}");
                    missionProgress[mission] = scrapCount;
                }
                // If already completed, do not change progress
            }
        }
        Debug.Log($"[MissionManager] Mission progress dict: {string.Join(", ", missionProgress)}");
    }

    public void UpdateEarnMoneyMission()
    {
        var moneyManager = FindObjectOfType<MoneyManager>();
        if (moneyManager == null)
        {
            Debug.LogWarning("[MissionManager] MoneyManager not found in scene.");
            return;
        }
        foreach (var mission in allMissions)
        {
            if (mission.missionName.ToLower().Contains("earn money"))
            {
                // Only update progress if not already completed
                if (!IsMissionCompleted(mission))
                {
                    int playerMoney = moneyManager.playerMoney;
                    Debug.Log($"[MissionManager] Updating mission '{mission.missionName}' with player money: {playerMoney}");
                    missionProgress[mission] = playerMoney;
                }
            }
        }
        Debug.Log($"[MissionManager] Mission progress dict: {string.Join(", ", missionProgress)}");
    }

    public void UpdateBuyItemMissions()
    {
        var inventory = FindObjectOfType<InventoryManager>();
        if (inventory == null)
        {
            Debug.LogWarning("[MissionManager] InventoryManager not found in scene.");
            return;
        }
        foreach (var mission in allMissions)
        {
            string lowerName = mission.missionName.ToLower();
            if (
                lowerName.Contains("buy a knife") ||
                lowerName.Contains("buy a bowl") ||
                lowerName.Contains("buy a fork") ||
                lowerName.Contains("buy a cutting board") ||
                lowerName.Contains("buy an avocado") ||
                lowerName.Contains("buy a cabbage") ||
                lowerName.Contains("buy a carrot") ||
                lowerName.Contains("buy an onion") ||
                lowerName.Contains("buy a paprika") ||
                lowerName.Contains("buy a tomato")
            )
            {
                if (!IsMissionCompleted(mission))
                {
                    string targetItem = "";
                    if (lowerName.Contains("knife")) targetItem = "knife";
                    else if (lowerName.Contains("bowl")) targetItem = "bowl";
                    else if (lowerName.Contains("fork")) targetItem = "fork";
                    else if (lowerName.Contains("cutting board")) targetItem = "cutting board";
                    else if (lowerName.Contains("avocado")) targetItem = "avocado";
                    else if (lowerName.Contains("cabbage")) targetItem = "cabbage";
                    else if (lowerName.Contains("carrot")) targetItem = "carrot";
                    else if (lowerName.Contains("onion")) targetItem = "onion";
                    else if (lowerName.Contains("paprika")) targetItem = "paprika";
                    else if (lowerName.Contains("tomato")) targetItem = "tomato";

                    int count = 0;
                    foreach (var slot in inventory.inventorySlots)
                    {
                        InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                        if (itemInSlot != null && itemInSlot.item != null && itemInSlot.item.name.ToLower().Contains(targetItem))
                        {
                            count = itemInSlot.count;
                            break;
                        }
                    }
                    Debug.Log($"[MissionManager] Updating mission '{mission.missionName}' with count: {count}");
                    missionProgress[mission] = count;
                }
            }
        }
        Debug.Log($"[MissionManager] Mission progress dict: {string.Join(", ", missionProgress)}");
    }

    // Notify the active mission display to update
    public void NotifyActiveMissionDisplay()
    {
        if (activeMissionDisplay != null)
        {
            activeMissionDisplay.UpdateDisplay();
        }
    }
}