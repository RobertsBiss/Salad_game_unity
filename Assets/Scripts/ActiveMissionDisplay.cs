using UnityEngine;
using TMPro;

public class ActiveMissionDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;

    [Header("Display Settings")]
    public bool showWhenNoActiveMissions = false;
    public string noMissionText = "No active missions";

    private Mission currentMission;
    private bool isVisible = false;

    void Start()
    {
        // Removed SetVisibility(false); to prevent hiding at start

        // Subscribe to mission updates
        if (MissionManager.Instance != null)
        {
            // We'll update this when missions change
            UpdateDisplay();
        }
    }

    void Update()
    {
        // Update display every frame to catch real-time changes
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (MissionManager.Instance == null) return;

        Mission newCurrentMission = GetCurrentActiveMission();

        // Check if we need to update the display
        if (newCurrentMission != currentMission)
        {
            currentMission = newCurrentMission;
            RefreshDisplay();
        }

        // Update progress for current mission
        if (currentMission != null)
        {
            UpdateProgress();
        }
    }

    private Mission GetCurrentActiveMission()
    {
        if (MissionManager.Instance == null || MissionManager.Instance.allMissions == null)
            return null;

        foreach (var mission in MissionManager.Instance.allMissions)
        {
            if (mission == null) continue;

            bool isActive = MissionManager.Instance.IsMissionActive(mission);
            bool isCompleted = MissionManager.Instance.IsMissionCompleted(mission);

            // Return the first active, non-completed mission
            if (isActive && !isCompleted)
            {
                return mission;
            }
        }

        return null;
    }

    private void RefreshDisplay()
    {
        if (currentMission != null)
        {
            // Show mission info
            if (missionNameText != null)
                missionNameText.text = currentMission.missionName;

            if (descriptionText != null)
                descriptionText.text = currentMission.description;

            SetVisibility(true);
        }
        else
        {
            // No active missions
            if (showWhenNoActiveMissions)
            {
                if (missionNameText != null)
                    missionNameText.text = noMissionText;
                if (descriptionText != null)
                    descriptionText.text = "";
                if (progressText != null)
                    progressText.text = "";
                SetVisibility(true);
            }
            else
            {
                SetVisibility(false);
            }
        }
    }

    private void UpdateProgress()
    {
        if (currentMission == null || progressText == null) return;

        int currentProgress = 0;
        if (MissionManager.Instance.missionProgress.ContainsKey(currentMission))
        {
            currentProgress = MissionManager.Instance.missionProgress[currentMission];
        }

        string progressString = $"{currentProgress}/{currentMission.requiredAmount}{currentMission.progressSuffix}";
        progressText.text = progressString;
    }

    private void SetVisibility(bool visible)
    {
        if (isVisible == visible) return;

        isVisible = visible;
        gameObject.SetActive(visible);
    }

    // Public method to force refresh (can be called from other scripts)
    public void ForceRefresh()
    {
        currentMission = null; // Force refresh
        UpdateDisplay();
    }
}