using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MissionItemUI : MonoBehaviour
{
    public TMP_Text missionNameText;
    public TMP_Text progressText;
    public TMP_Text statusText;
    public TMP_Text descriptionText;

    public void Setup(Mission mission, int progress, bool isActive, bool isCompleted)
    {
        Debug.Log($"[MissionItemUI] Setup called for {mission.missionName} with progress {progress}/{mission.requiredAmount}");
        missionNameText.text = mission.missionName;
        progressText.text = $"{progress}/{mission.requiredAmount}{mission.progressSuffix}";
        if (descriptionText != null)
            descriptionText.text = mission.description;
        if (isCompleted)
        {
            statusText.text = "Completed";
            statusText.color = Color.green;
        }
        else if (isActive)
        {
            statusText.text = "Active";
            statusText.color = Color.blue;
        }
        else
        {
            statusText.text = "Locked";
            statusText.color = Color.gray;
        }
    }
}