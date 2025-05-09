using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float runSpeed = 10.0f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpForce = 5.0f;
    [SerializeField] private float gravity = 18.0f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float crouchingHeight = 1.0f;
    [SerializeField] private float crouchTransitionSpeed = 10.0f;
    [SerializeField] private Vector3 standingCameraPosition = new Vector3(0, 0.6f, 0);
    [SerializeField] private Vector3 crouchingCameraPosition = new Vector3(0, 0.2f, 0);

    [Header("Look Settings")]
    [SerializeField] private float lookSensitivity = 2.0f;
    [SerializeField] private float lookSmoothTime = 0.03f;
    [SerializeField] private float lookXLimit = 80.0f;
    [SerializeField] private Transform cameraHolder;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private float currentSpeed;
    private Vector2 currentLookDelta = Vector2.zero;
    private Vector2 lookDeltaVelocity = Vector2.zero;
    private bool canJump = true;
    private bool isCrouching = false;
    private float currentHeight;
    private Vector3 currentCameraPosition;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // If no camera holder is assigned, create one
        if (cameraHolder == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraHolder = mainCamera.transform;
            }
        }

        // Initialize camera and character controller heights
        currentHeight = standingHeight;
        currentCameraPosition = standingCameraPosition;
        if (cameraHolder != null)
        {
            cameraHolder.localPosition = currentCameraPosition;
        }

        // Set character controller initial height
        characterController.height = currentHeight;
        characterController.center = new Vector3(0, currentHeight / 2, 0);

        // Lock cursor to center of screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Set initial speed
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        // Basic ground check
        bool isGrounded = characterController.isGrounded;

        // Handle crouching
        HandleCrouch();

        // Look handling
        HandleLooking();

        // Handle movement
        if (isGrounded)
        {
            // Get input values
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            // Convert to world space direction relative to player orientation
            Vector3 forward = transform.forward * verticalInput;
            Vector3 right = transform.right * horizontalInput;

            // Combine movement vectors and normalize
            Vector3 move = (forward + right).normalized;

            // Only normalize if there's actually input to prevent NaN errors
            if (move.magnitude > 0)
            {
                move = move.normalized;
            }

            // Set speed based on crouch and sprint states
            if (isCrouching)
            {
                currentSpeed = crouchSpeed;
            }
            else
            {
                currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            }

            // Apply speed to movement
            moveDirection = move * currentSpeed;

            // Apply jump force if jump is pressed and not crouching
            if (canJump && !isCrouching && Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
            }
        }

        // Apply gravity regardless of whether we're grounded or not
        moveDirection.y -= gravity * Time.deltaTime;

        // If grounded and not jumping, ensure we're not falling
        if (isGrounded && moveDirection.y < 0)
        {
            moveDirection.y = -0.3f; // Small downward force to keep grounded
        }

        // Apply movement
        characterController.Move(moveDirection * Time.deltaTime);
    }

    void HandleCrouch()
    {
        // Toggle crouch state when C key is pressed
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }

        // Set target height and camera position based on crouch state
        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        Vector3 targetCameraPosition = isCrouching ? crouchingCameraPosition : standingCameraPosition;

        // Smoothly transition between heights
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Apply to character controller
        characterController.height = currentHeight;
        characterController.center = new Vector3(0, currentHeight / 2, 0);

        // Update camera position
        if (cameraHolder != null)
        {
            currentCameraPosition = Vector3.Lerp(cameraHolder.localPosition, targetCameraPosition, crouchTransitionSpeed * Time.deltaTime);
            cameraHolder.localPosition = currentCameraPosition;
        }
    }

    void HandleLooking()
    {
        if (cameraHolder == null) return;

        // Get mouse input - using GetAxisRaw for more direct response
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        // Apply mouse sensitivity
        mouseX *= lookSensitivity;
        mouseY *= lookSensitivity;

        // Smooth the mouse input (reduced smoothing for more responsive feel)
        Vector2 targetLookDelta = new Vector2(mouseX, mouseY);
        currentLookDelta = Vector2.SmoothDamp(currentLookDelta, targetLookDelta, ref lookDeltaVelocity, lookSmoothTime);

        // Rotate the player horizontally (left/right)
        transform.Rotate(Vector3.up * currentLookDelta.x);

        // Rotate the camera vertically (up/down)
        rotationX -= currentLookDelta.y;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        cameraHolder.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    // Method to toggle cursor lock (useful for menus)
    public void ToggleCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // Method to toggle jumping ability
    public void SetJumpAbility(bool canPlayerJump)
    {
        canJump = canPlayerJump;
    }
}