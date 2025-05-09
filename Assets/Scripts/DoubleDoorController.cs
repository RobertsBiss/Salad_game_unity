using UnityEngine;

public class DoubleDoorController : MonoBehaviour
{
    public Transform leftDoorHinge;
    public Transform rightDoorHinge;
    public Transform player;
    public float openAngle = 90f;
    public float openSpeed = 4f;
    public float interactionDistance = 3f;

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
        if (Vector3.Distance(player.position, transform.position) < interactionDistance && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;

            Vector3 toPlayer = (player.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, toPlayer);
            float angleDir = (dot > 0) ? -1 : 1;

            // Recalculate open rotations dynamically
            leftOpenRot = leftClosedRot * Quaternion.Euler(0, angleDir * openAngle, 0);
            rightOpenRot = rightClosedRot * Quaternion.Euler(0, -angleDir * openAngle, 0);
        }

        // Smoothly rotate the doors
        leftDoorHinge.localRotation = Quaternion.Slerp(leftDoorHinge.localRotation, isOpen ? leftOpenRot : leftClosedRot, Time.deltaTime * openSpeed);
        rightDoorHinge.localRotation = Quaternion.Slerp(rightDoorHinge.localRotation, isOpen ? rightOpenRot : rightClosedRot, Time.deltaTime * openSpeed);
    }
}