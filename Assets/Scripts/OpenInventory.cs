using UnityEngine;
using UnityEngine.UI;

public class OpenInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject mainInventoryGroup;  // Assign the MainInventoryGroup GameObject
    [SerializeField] private KeyCode toggleKey = KeyCode.I;  // Key to toggle inventory
    [SerializeField] private FirstPersonController firstPersonController; // Assign your FirstPersonController here

    [Header("UI Interaction Fix")]
    [SerializeField] private GameObject interactionPromptObject; // Assign your InteractionPrompt GameObject here

    private bool isInventoryOpen = false;

    void Start()
    {
        // Hide inventory at start
        if (mainInventoryGroup != null)
        {
            mainInventoryGroup.SetActive(false);
        }

        // Find FirstPersonController if not assigned
        if (firstPersonController == null)
        {
            firstPersonController = FindObjectOfType<FirstPersonController>();
        }

        // Setup interaction prompt canvas group
        SetupInteractionPrompt();

        // Lock cursor at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SetupInteractionPrompt()
    {
        // Find interaction prompt if not assigned
        if (interactionPromptObject == null)
        {
            // Try to find it by name
            interactionPromptObject = GameObject.Find("InteractionPrompt");
        }
    }

    void Update()
    {
        // Check for toggle key press
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }

        // Manage interaction prompt blocking based on inventory state
        UpdateInteractionPromptBlocking();
    }

    void UpdateInteractionPromptBlocking()
    {
        if (interactionPromptObject != null)
        {
            // Simply disable the entire interaction prompt when inventory is open
            interactionPromptObject.SetActive(!isInventoryOpen);
        }
    }

    public void ToggleInventory()
    {
        if (mainInventoryGroup == null)
        {
            Debug.LogError("OpenInventory: Cannot toggle - MainInventoryGroup is null!");
            return;
        }

        isInventoryOpen = !isInventoryOpen;
        mainInventoryGroup.SetActive(isInventoryOpen);

        // Toggle cursor state
        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Enable/Disable player controls (but keep the component enabled for stamina updates)
        if (firstPersonController != null)
        {
            firstPersonController.SetControlsEnabled(!isInventoryOpen);
        }
        else
        {
            Debug.LogWarning("OpenInventory: FirstPersonController reference is missing!");
        }

        // Update interaction prompt blocking
        UpdateInteractionPromptBlocking();
    }

    // Public method to check if inventory is open (useful for other scripts)
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    // Public method to close inventory (useful for other scripts)
    public void CloseInventory()
    {
        if (isInventoryOpen)
        {
            ToggleInventory();
        }
    }
}