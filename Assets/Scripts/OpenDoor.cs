using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public Transform player;
    public float interactDistance = 3f;
    public KeyCode openKey = KeyCode.E;
    public Camera playerCamera;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioSource audioSource;

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
        if (playerCamera != null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                if (hit.collider != null && (hit.collider.transform == this.transform || hit.collider.transform.IsChildOf(this.transform)))
                {
                    if (Input.GetKeyDown(openKey))
                    {
                        ToggleDoorDirection();
                    }
                }
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
            if (audioSource != null && openSound != null)
                audioSource.PlayOneShot(openSound);
        }
        else
        {
            // Close the door
            targetRotation = closedRotation;
            if (audioSource != null && closeSound != null)
                audioSource.PlayOneShot(closeSound);
        }

        isOpen = !isOpen;
    }
}