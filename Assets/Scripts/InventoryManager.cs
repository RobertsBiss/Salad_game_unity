using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject mainInventoryGroup;  // Assign the MainInventoryGroup GameObject
    [SerializeField] private KeyCode toggleKey = KeyCode.I;  // Key to toggle inventory

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
        if (isInventoryOpen)
        {
            // Show and unlock cursor when inventory is open
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Hide and lock cursor when inventory is closed
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}