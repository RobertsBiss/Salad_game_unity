using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public Transform door; // The door object that will slide
    public Vector3 openOffset; // The distance the door should slide open (e.g., along the X-axis)
    public float speed = 2f; // Speed at which the door slides open/close
    public float interactDistance = 3f; // Distance from the door at which interaction is possible (default is 3)
    public KeyCode openKey = KeyCode.E; // Key to open the door, defaults to 'E'
    public Transform player; // Reference to the player's transform

    private Vector3 closedPosition;
    private bool isOpen = false;

    private void Start()
    {
        // Store the door's starting position (closed position)
        closedPosition = door.position;
    }

    private void Update()
    {
        // Check if the player is within interaction distance
        if (Vector3.Distance(transform.position, player.position) <= interactDistance)
        {
            if (Input.GetKeyDown(openKey))
            {
                ToggleDoor();
            }
        }

        // Move the door based on its current state (open or closed)
        if (isOpen)
        {
            // Move the door to the open position
            door.position = Vector3.MoveTowards(door.position, closedPosition + openOffset, speed * Time.deltaTime);
        }
        else
        {
            // Move the door back to the closed position
            door.position = Vector3.MoveTowards(door.position, closedPosition, speed * Time.deltaTime);
        }
    }

    // Method to toggle the door's state (open/close)
    public void ToggleDoor()
    {
        isOpen = !isOpen; // Toggle between open and closed states
    }
}