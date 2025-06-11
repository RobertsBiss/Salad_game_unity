using UnityEngine;

public class OpenInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject mainInventoryGroup;  // Assign the MainInventoryGroup GameObject
    [SerializeField] private KeyCode toggleKey = KeyCode.I;  // Key to toggle inventory
    [SerializeField] private FirstPersonController firstPersonController; // Assign your FirstPersonController here

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

        // Lock cursor at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Check for toggle key press
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
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