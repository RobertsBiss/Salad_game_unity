using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public Transform player;
    public float interactDistance = 3f;
    public KeyCode openKey = KeyCode.E;

    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private bool isOpen = false;

    void Start()
    {
        closedRotation = transform.rotation;
        targetRotation = closedRotation;
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, player.position) <= interactDistance)
        {
            if (Input.GetKeyDown(openKey))
            {
                ToggleDoorDirection();
            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);
    }

    void ToggleDoorDirection()
    {
        if (!isOpen)
        {
            // Get direction from door to player
            Vector3 toPlayer = (player.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, toPlayer);

            // Flip the logic: open away from player
            float angle = (dot > 0) ? openAngle : -openAngle;
            targetRotation = Quaternion.Euler(0, angle, 0) * closedRotation;
        }
        else
        {
            // Close the door
            targetRotation = closedRotation;
        }

        isOpen = !isOpen;
    }
}