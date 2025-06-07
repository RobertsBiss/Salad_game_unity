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

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 5f;
    [SerializeField] private float staminaRegenRate = 1f;
    [SerializeField] private float staminaDrainRate = 1.5f;
    [SerializeField] private float minStaminaToSprint = 1f;
    public float currentStamina { get; private set; }
    public bool isSprinting { get; private set; }

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

        if (cameraHolder == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraHolder = mainCamera.transform;
            }
        }

        currentHeight = standingHeight;
        currentCameraPosition = standingCameraPosition;
        if (cameraHolder != null)
        {
            cameraHolder.localPosition = currentCameraPosition;
        }

        characterController.height = currentHeight;
        characterController.center = new Vector3(0, currentHeight / 2, 0);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentSpeed = walkSpeed;
        currentStamina = maxStamina;
    }

    void Update()
    {
        bool isGrounded = characterController.isGrounded;

        HandleCrouch();
        HandleLooking();

        if (isGrounded)
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            Vector3 forward = transform.forward * verticalInput;
            Vector3 right = transform.right * horizontalInput;
            Vector3 move = (forward + right).normalized;

            if (move.magnitude > 0)
            {
                move = move.normalized;
            }

            bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && !isCrouching && move != Vector3.zero;

            if (wantsToSprint && currentStamina > minStaminaToSprint)
            {
                isSprinting = true;
                currentSpeed = runSpeed;
                currentStamina -= staminaDrainRate * Time.deltaTime;
            }
            else
            {
                isSprinting = false;
                currentSpeed = walkSpeed;
                if (!wantsToSprint && currentStamina < maxStamina)
                {
                    currentStamina += staminaRegenRate * Time.deltaTime;
                }
            }

            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

            if (isCrouching)
            {
                currentSpeed = crouchSpeed;
            }

            moveDirection = move * currentSpeed;

            if (canJump && !isCrouching && Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
            }
        }

        moveDirection.y -= gravity * Time.deltaTime;

        if (isGrounded && moveDirection.y < 0)
        {
            moveDirection.y = -0.3f;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }

        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        Vector3 targetCameraPosition = isCrouching ? crouchingCameraPosition : standingCameraPosition;

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        characterController.height = currentHeight;
        characterController.center = new Vector3(0, currentHeight / 2, 0);

        if (cameraHolder != null)
        {
            currentCameraPosition = Vector3.Lerp(cameraHolder.localPosition, targetCameraPosition, crouchTransitionSpeed * Time.deltaTime);
            cameraHolder.localPosition = currentCameraPosition;
        }
    }

    void HandleLooking()
    {
        if (cameraHolder == null) return;

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        mouseX *= lookSensitivity;
        mouseY *= lookSensitivity;

        Vector2 targetLookDelta = new Vector2(mouseX, mouseY);
        currentLookDelta = Vector2.SmoothDamp(currentLookDelta, targetLookDelta, ref lookDeltaVelocity, lookSmoothTime);

        transform.Rotate(Vector3.up * currentLookDelta.x);

        rotationX -= currentLookDelta.y;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        cameraHolder.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    public void ToggleCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void SetJumpAbility(bool canPlayerJump)
    {
        canJump = canPlayerJump;
    }
}