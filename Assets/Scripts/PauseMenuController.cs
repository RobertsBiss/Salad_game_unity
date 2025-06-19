using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    // Assign your menu container (the parent Image of MainMenu) in the inspector
    public GameObject menuContainer;
    // Optionally assign your player controller, or it will be found automatically
    public FirstPersonController playerController;
    private OpenInventory openInventory;

    void Start()
    {
        // Find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
        }
        openInventory = FindFirstObjectByType<OpenInventory>();
    }

    void Update()
    {
        // Only allow Escape toggling in the main game scene (not in MainMenu scene)
        if (menuContainer != null && SceneManager.GetActiveScene().name != "MainMenu")
        {
            // Only allow Escape to toggle menu if no shop or mission UI is open
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (openInventory != null && (IsAnyShopOpen() || IsAnyMissionOpen()))
                {
                    // Do nothing, let shop/mission UI handle Escape
                    return;
                }
                bool isActive = menuContainer.activeSelf;
                menuContainer.SetActive(!isActive);
                HandlePauseState(!isActive);
            }
        }
    }

    bool IsAnyShopOpen()
    {
        return openInventory != null && (bool)openInventory.GetType().GetMethod("IsAnyShopOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(openInventory, null);
    }

    bool IsAnyMissionOpen()
    {
        return openInventory != null && (bool)openInventory.GetType().GetMethod("IsAnyMissionOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(openInventory, null);
    }

    void HandlePauseState(bool paused)
    {
        // Pause/unpause player controls and cursor
        if (playerController != null)
        {
            playerController.SetControlsEnabled(!paused);
        }
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    public void ResumeGame()
    {
        if (menuContainer != null)
        {
            menuContainer.SetActive(false);
        }
        HandlePauseState(false);
    }
}