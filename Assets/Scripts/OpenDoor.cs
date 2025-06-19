using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public Transform player;
    public float interactDistance = 3f;
    public KeyCode openKey = KeyCode.E;

    [Header("Audio")]
    public AudioSource doorAudioSource; // Reference to the door's AudioSource
    public AudioClip doorOpenSound; // Sound to play when door opens
    public AudioClip doorCloseSound; // Sound to play when door closes
    public float doorSoundVolume = 0.7f; // Volume for door sounds

    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private bool isOpen = false;
    private bool wasOpen = false; // Track previous state to detect changes

    void Start()
    {
        closedRotation = transform.rotation;
        targetRotation = closedRotation;

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
        if (Vector3.Distance(transform.position, player.position) <= interactDistance)
        {
            if (Input.GetKeyDown(openKey))
            {
                ToggleDoorDirection();
            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);

        // Check for state changes to play sounds
        if (isOpen != wasOpen)
        {
            PlayDoorSound(isOpen);
            wasOpen = isOpen;
        }
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