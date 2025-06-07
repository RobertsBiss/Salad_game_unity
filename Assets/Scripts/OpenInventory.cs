using UnityEngine;

public class OpenInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject mainInventoryGroup;  // Assign the MainInventoryGroup GameObject
    [SerializeField] private KeyCode toggleKey = KeyCode.I;  // Key to toggle inventory
    [SerializeField] private MonoBehaviour cameraController; // Assign your camera controller script here

    private bool isInventoryOpen = false;

    void Start()
    {
        // Hide inventory at start
        if (mainInventoryGroup != null)
        {
            mainInventoryGroup.SetActive(false);
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
            Debug.LogError("InventoryManager: Cannot toggle - MainInventoryGroup is null!");
            return;
        }

        isInventoryOpen = !isInventoryOpen;
        mainInventoryGroup.SetActive(isInventoryOpen);

        // Toggle cursor state
        Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isInventoryOpen;

        // Enable/Disable camera controller
        if (cameraController != null)
        {
            cameraController.enabled = !isInventoryOpen;
        }
    }
}