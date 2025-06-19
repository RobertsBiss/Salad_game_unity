using UnityEngine;

public class DoubleDoorController : MonoBehaviour
{
    public Transform leftDoorHinge;
    public Transform rightDoorHinge;
    public Transform player;
    public float openAngle = 90f;
    public float openSpeed = 4f;
    public float interactionDistance = 3f;
    public Camera playerCamera;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioSource audioSource;

    private Quaternion leftClosedRot, leftOpenRot;
    private Quaternion rightClosedRot, rightOpenRot;
    private bool isOpen = false;

    void Start()
    {
        leftClosedRot = leftDoorHinge.localRotation;
        rightClosedRot = rightDoorHinge.localRotation;
    }

    void Update()
    {
        if (playerCamera != null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                if (hit.collider != null && (hit.collider.transform == this.transform || hit.collider.transform.IsChildOf(this.transform)))
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        isOpen = !isOpen;
                        Vector3 toPlayer = (player.position - transform.position).normalized;
                        float dot = Vector3.Dot(transform.forward, toPlayer);
                        float angleDir = (dot > 0) ? -1 : 1;
                        // Recalculate open rotations dynamically
                        leftOpenRot = leftClosedRot * Quaternion.Euler(0, angleDir * openAngle, 0);
                        rightOpenRot = rightClosedRot * Quaternion.Euler(0, -angleDir * openAngle, 0);
                        // Play open/close sound
                        if (audioSource != null)
                        {
                            if (isOpen && openSound != null)
                                audioSource.PlayOneShot(openSound);
                            else if (!isOpen && closeSound != null)
                                audioSource.PlayOneShot(closeSound);
                        }
                    }
                }
            }
        }

        // Smoothly rotate the doors
        leftDoorHinge.localRotation = Quaternion.Slerp(leftDoorHinge.localRotation, isOpen ? leftOpenRot : leftClosedRot, Time.deltaTime * openSpeed);
        rightDoorHinge.localRotation = Quaternion.Slerp(rightDoorHinge.localRotation, isOpen ? rightOpenRot : rightClosedRot, Time.deltaTime * openSpeed);
    }
}