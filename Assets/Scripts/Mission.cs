using UnityEngine;

[CreateAssetMenu(menuName = "Mission System/Mission", fileName = "NewMission")]
public class Mission : ScriptableObject
{
    public string missionName;
    [TextArea] public string description;
    public int requiredAmount;
    public string progressSuffix; // e.g., "$" for money, "" for items
    public Mission[] prerequisites; // Optional: for mission chains, supports multiple prerequisites
}