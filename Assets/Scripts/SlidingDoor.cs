using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public Transform door; // The door object that will slide
    public Vector3 openOffset; // The distance the door should slide open (e.g., along the X-axis)
    public float speed = 2f; // Speed at which the door slides open/close
    public float interactDistance = 3f; // Distance from the door at which interaction is possible (default is 3)
    public KeyCode openKey = KeyCode.E; // Key to open the door, defaults to 'E'
    public Transform player; // Reference to the player's transform

    [Header("Audio")]
    public AudioSource doorAudioSource; // Reference to the door's AudioSource
    public AudioClip doorOpenSound; // Sound to play when door opens
    public AudioClip doorCloseSound; // Sound to play when door closes
    public float doorSoundVolume = 0.7f; // Volume for door sounds

    private Vector3 closedPosition;
    private bool isOpen = false;
    private bool wasOpen = false; // Track previous state to detect changes

    private void Start()
    {
        // Store the door's starting position (closed position)
        closedPosition = door.position;

        // Find AudioSource if not assigned
        if (doorAudioSource == null)
        {
            doorAudioSource = GetComponent<AudioSource>();
            if (doorAudioSource == null)
            {
                doorAudioSource = door.GetComponent<AudioSource>();
            }
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

        // Check for state changes to play sounds
        if (isOpen != wasOpen)
        {
            PlayDoorSound(isOpen);
            wasOpen = isOpen;
        }
    }

    // Method to toggle the door's state (open/close)
    public void ToggleDoor()
    {
        isOpen = !isOpen; // Toggle between open and closed states
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