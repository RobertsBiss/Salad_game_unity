using UnityEngine;
using TMPro; // If you're using TextMeshPro

public class DoorInteractionScript : MonoBehaviour
{
    public float interactionDistance = 3f;
    public string doorTag = "Door";
    public TMP_Text interactionText; // Assign in inspector
    public Camera playerCamera;

    private void Update()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Raycast normally (no layer mask)
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            if (hit.collider.CompareTag(doorTag))
            {
                // Show the interaction text
                interactionText.text = "[E] Open";
                interactionText.color = new Color(1, 1, 1, 1); // Fully visible
                return;
            }
        }

        // Hide the interaction text
        interactionText.color = new Color(1, 1, 1, 0); // Fully transparent
    }
}