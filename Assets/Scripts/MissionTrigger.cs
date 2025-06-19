using UnityEngine;
using System.Collections;

public class MissionTrigger : MonoBehaviour
{
    [Header("Mission UI")]
    public GameObject missionContainer; // Drag your MissionContainer GameObject here in the inspector

    [Header("Player Tag (Optional)")]
    public string playerTag = "Player"; // Change this if your player has a different tag

    [Header("Player & Camera Scripts")]
    public MonoBehaviour playerController; // Assign your player movement script here
    public MonoBehaviour cameraController; // Assign your camera script here

    [Header("Animation Settings")]
    public float delayBeforeDisable = 0.5f; // Time to wait before disabling player controller

    [Header("Input Settings")]
    public KeyCode closeMissionKey = KeyCode.Escape; // Key to close the mission screen

    private bool missionOpen = false;
    private bool isOpeningMission = false;

    void Start()
    {
        // Find mission container if not assigned
        if (missionContainer == null)
        {
            missionContainer = GameObject.Find("MissionContainer");
            if (missionContainer == null)
            {
                Debug.LogError("MissionTrigger: MissionContainer not found! Please assign it in the inspector or make sure it exists in the scene.");
                enabled = false;
                return;
            }
        }

        // Find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
        }

        // Find camera controller if not assigned
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<FirstPersonController>();
        }

        // Hide mission container at start
        if (missionContainer != null)
        {
            missionContainer.SetActive(false);
        }
    }

    void Update()
    {
        // Check for input to close mission screen
        if (Input.GetKeyDown(closeMissionKey) && missionOpen)
        {
            CloseMissionScreen();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the player
        if (other.CompareTag(playerTag) && !missionOpen && !isOpeningMission)
        {
            OpenMissionScreen();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if the exiting object is the player
        if (other.CompareTag(playerTag))
        {
            // No need to set playerInRange to false as it's not used in the new code
        }
    }

    void OpenMissionScreen()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.PopulateMissionList();
        if (missionContainer == null) return;

        isOpeningMission = true;
        missionOpen = true;

        // Show mission container
        missionContainer.SetActive(true);

        // Disable player controller after a short delay
        if (playerController != null)
        {
            StartCoroutine(DisablePlayerController());
        }

        // Disable camera controller
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        // Lock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Mission screen opened!");
    }

    // Public method to close the mission screen that can be called from UI buttons
    public void CloseMissionButton()
    {
        CloseMissionScreen();
    }

    void CloseMissionScreen()
    {
        if (missionContainer == null) return;

        missionOpen = false;
        isOpeningMission = false;

        // Hide mission container
        missionContainer.SetActive(false);

        // Re-enable player controller
        if (playerController != null)
        {
            var fpc = playerController as FirstPersonController;
            if (fpc != null)
            {
                fpc.SetMovementEnabled(true);
                fpc.SetLookingEnabled(true);
            }
            else
                playerController.enabled = true;
        }

        // Re-enable camera controller
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        // Lock cursor back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Mission screen closed!");
    }

    IEnumerator DisablePlayerController()
    {
        yield return new WaitForSeconds(delayBeforeDisable);
        if (playerController != null)
        {
            // Use SetMovementEnabled if available, otherwise fallback to disabling
            var fpc = playerController as FirstPersonController;
            if (fpc != null)
            {
                fpc.SetMovementEnabled(false);
                fpc.SetLookingEnabled(false);
            }
            else
                playerController.enabled = false;
        }
        isOpeningMission = false;
    }

    // Optional: Visual feedback in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}