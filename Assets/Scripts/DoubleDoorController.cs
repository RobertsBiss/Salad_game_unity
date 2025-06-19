using UnityEngine;

public class DoubleDoorController : MonoBehaviour
{
    public Transform leftDoorHinge;
    public Transform rightDoorHinge;
    public Transform player;
    public float openAngle = 90f;
    public float openSpeed = 4f;
    public float interactionDistance = 3f;

    [Header("Audio")]
    public AudioSource doorAudioSource; // Reference to the door's AudioSource
    public AudioClip doorOpenSound; // Sound to play when doors open
    public AudioClip doorCloseSound; // Sound to play when doors close
    public float doorSoundVolume = 0.7f; // Volume for door sounds

    private Quaternion leftClosedRot, leftOpenRot;
    private Quaternion rightClosedRot, rightOpenRot;
    private bool isOpen = false;
    private bool wasOpen = false; // Track previous state to detect changes

    void Start()
    {
        leftClosedRot = leftDoorHinge.localRotation;
        rightClosedRot = rightDoorHinge.localRotation;

        // Find AudioSource if not assigned
        if (doorAudioSource == null)
        {
            doorAudioSource = GetComponent<AudioSource>();
        }

        // Configure AudioSource if found
        if (doorAudioSource != null)
        {
            doorAudioSource.playOnAwake = false;
            doorAudioSource.loop = false;
            doorAudioSource.spatialBlend = 1f; // 3D sound
            doorAudioSource.minDistance = 3f;
            doorAudioSource.maxDistance = 20f;
        }
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

        // Check for state changes to play sounds
        if (isOpen != wasOpen)
        {
            PlayDoorSound(isOpen);
            wasOpen = isOpen;
        }
    }

    // Method to play door sounds
    private void PlayDoorSound(bool opening)
    {
        if (doorAudioSource == null) return;

        AudioClip soundToPlay = opening ? doorOpenSound : doorCloseSound;
        if (soundToPlay != null)
        {
            doorAudioSource.clip = soundToPlay;
            doorAudioSource.volume = doorSoundVolume;
            doorAudioSource.Play();
        }
    }

    // Public method to manually trigger door sounds (useful for testing)
    [ContextMenu("Test Door Open Sound")]
    public void TestOpenSound()
    {
        if (doorAudioSource != null && doorOpenSound != null)
        {
            doorAudioSource.clip = doorOpenSound;
            doorAudioSource.volume = doorSoundVolume;
            doorAudioSource.Play();
        }
    }

    [ContextMenu("Test Door Close Sound")]
    public void TestCloseSound()
    {
        if (doorAudioSource != null && doorCloseSound != null)
        {
            doorAudioSource.clip = doorCloseSound;
            doorAudioSource.volume = doorSoundVolume;
            doorAudioSource.Play();
        }
    }
}