using UnityEngine;
using System.Collections;

public class ShopTrigger : MonoBehaviour
{
    [Header("Shop UI")]
    public GameObject mainShop; // Drag your MainShop GameObject here in the inspector
    public GameObject shopContainer; // Drag your ShopContainer here in the inspector

    [Header("Player Tag (Optional)")]
    public string playerTag = "Player"; // Change this if your player has a different tag

    [Header("Player & Camera Scripts")]
    public MonoBehaviour playerController; // Assign your player movement script here
    public MonoBehaviour cameraController; // Assign your camera script here

    [Header("Shop Inventory")]
    public ShopInventory shopInventory; // Assign a ShopInventory asset for this shop

    [Header("Shop UI Manager")]
    public ShopUIManager shopUIManager; // Assign your ShopUIManager in the inspector

    [Header("Animation Settings")]
    public float delayBeforeDisable = 0.5f; // Time to wait before disabling player controller

    [Header("Input Settings")]
    public KeyCode closeShopKey = KeyCode.E; // Key to close the shop

    private bool shopOpen = false;
    private OpenInventory openInventory;
    private bool isOpeningShop = false;

    private void Start()
    {
        // Make sure the shop is initially disabled
        if (mainShop != null)
        {
            mainShop.SetActive(false);
        }

        // Find OpenInventory component
        openInventory = FindObjectOfType<OpenInventory>();
    }

    private void Update()
    {
        if (shopOpen && Input.GetKeyDown(closeShopKey))
        {
            CloseShop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isOpeningShop)
        {
            StartCoroutine(OpenShopWithDelay());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            CloseShop();
        }
    }

    private IEnumerator OpenShopWithDelay()
    {
        isOpeningShop = true;

        // Show UI immediately
        if (mainShop != null)
            mainShop.SetActive(true);

        // Populate the shop UI with this shop's inventory
        if (shopUIManager != null && shopInventory != null)
            shopUIManager.OpenShop(shopInventory);

        shopOpen = true;

        // Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeDisable);

        // Disable player and camera controls after delay
        if (playerController != null)
            playerController.enabled = false;
        if (cameraController != null)
            cameraController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Update crosshair visibility
        if (openInventory != null)
        {
            openInventory.UpdateCrosshairVisibility();
        }

        isOpeningShop = false;
    }

    // Public method to close the shop that can be called from UI buttons
    public void CloseShopButton()
    {
        CloseShop();
    }

    private void CloseShop()
    {
        if (mainShop != null)
            mainShop.SetActive(false);

        shopOpen = false;

        if (playerController != null)
            playerController.enabled = true;
        if (cameraController != null)
            cameraController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Update crosshair visibility
        if (openInventory != null)
        {
            openInventory.UpdateCrosshairVisibility();
        }
    }
}