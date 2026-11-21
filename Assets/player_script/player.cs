using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public interface IInteractable
{
    string PromptText { get; }
    bool CanInteract(GameObject interactor);
    void Interact(GameObject interactor);
}

public enum MovementMode
{
    Sneak,
    Walk,
    Sprint
}

[RequireComponent(typeof(CharacterController))]
public class player : MonoBehaviour
{
    [Header("Movement Speeds (m/s)")]
    [SerializeField] private float sneakSpeed = 2.0f;
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float gravity = -20f;

    [Header("Noise Emission (Meters)")]
    [SerializeField] private float sneakNoiseRadius = 1.5f;
    [SerializeField] private float walkNoiseRadius = 6.0f;
    [SerializeField] private float sprintNoiseRadius = 14.0f;
    [SerializeField] private float noiseEmitInterval = 0.35f;

    [Header("Aiming & Rotation")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float turnSpeed = 25f;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactableMask = ~0;

    public MovementMode CurrentMode { get; private set; } = MovementMode.Walk;
    public Vector3 AimWorldPoint { get; private set; }
    public IInteractable HoveredInteractable { get; private set; }

    public event Action<Vector3, float, MovementMode> OnNoiseEmitted;

    private CharacterController _controller;
    private Vector3 _currentHorizontalVelocity;
    private float _verticalVelocity;
    private float _noiseTimer;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        AimWorldPoint = transform.position + transform.forward;
    }

    private void Update()
    {
        Vector2 moveInput = ReadMoveInput();
        bool isSneakHeld = ReadSneakHeld();
        bool isSprintHeld = ReadSprintHeld();
        bool interactPressed = ReadInteractPressed();
        Vector2 mouseScreenPos = ReadMousePosition();

        UpdateMovementMode(moveInput.sqrMagnitude > 0.01f, isSneakHeld, isSprintHeld);
        HandleMovement(moveInput);
        HandleAimAndRotation(mouseScreenPos);
        HandleInteraction(mouseScreenPos, interactPressed);
        HandleNoiseEmission();
    }

    private void UpdateMovementMode(bool isMoving, bool isSneakHeld, bool isSprintHeld)
    {
        if (!isMoving)
        {
            CurrentMode = isSneakHeld ? MovementMode.Sneak : MovementMode.Walk;
            return;
        }

        if (isSprintHeld)
        {
            CurrentMode = MovementMode.Sprint;
        }
        else if (isSneakHeld)
        {
            CurrentMode = MovementMode.Sneak;
        }
        else
        {
            CurrentMode = MovementMode.Walk;
        }
    }

    private void HandleMovement(Vector2 moveInput)
    {
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        float targetSpeed = GetSpeedForMode(CurrentMode);
        Vector3 desiredHorizontalVel = inputDir * targetSpeed;

        _currentHorizontalVelocity = Vector3.Lerp(
            _currentHorizontalVelocity,
            desiredHorizontalVel,
            acceleration * Time.deltaTime
        );

        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 motion = _currentHorizontalVelocity;
        motion.y = _verticalVelocity;

        _controller.Move(motion * Time.deltaTime);
    }

    private void HandleAimAndRotation(Vector2 mouseScreenPos)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (!aimPlane.Raycast(ray, out float enterDistance)) return;

        AimWorldPoint = ray.GetPoint(enterDistance);
        Vector3 lookDir = AimWorldPoint - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }

    private void HandleInteraction(Vector2 mouseScreenPos, bool interactPressed)
    {
        HoveredInteractable = null;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 300f, interactableMask))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                Vector3 closestPoint = hit.collider.ClosestPoint(transform.position);
                float distToPlayer = Vector3.Distance(transform.position, closestPoint);

                if (distToPlayer <= interactRange && interactable.CanInteract(gameObject))
                {
                    HoveredInteractable = interactable;

                    if (interactPressed)
                    {
                        interactable.Interact(gameObject);
                    }
                }
            }
        }
    }

    private void HandleNoiseEmission()
    {
        Vector3 horizontalVel = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
        if (horizontalVel.magnitude <= 0.2f)
        {
            _noiseTimer = 0f;
            return;
        }

        _noiseTimer -= Time.deltaTime;
        if (_noiseTimer <= 0f)
        {
            _noiseTimer = noiseEmitInterval;
            float radius = GetNoiseRadiusForMode(CurrentMode);
            OnNoiseEmitted?.Invoke(transform.position, radius, CurrentMode);
        }
    }

    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            return new Vector2(x, y);
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }

    private bool ReadSneakHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed))
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
        {
            return true;
        }
#endif
        return false;
    }

    private bool ReadSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftShift))
        {
            return true;
        }
#endif
        return false;
    }

    private bool ReadInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }
#endif
        return false;
    }

    private Vector2 ReadMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private float GetSpeedForMode(MovementMode mode) => mode switch
    {
        MovementMode.Sneak => sneakSpeed,
        MovementMode.Sprint => sprintSpeed,
        _ => walkSpeed
    };

    private float GetNoiseRadiusForMode(MovementMode mode) => mode switch
    {
        MovementMode.Sneak => sneakNoiseRadius,
        MovementMode.Sprint => sprintNoiseRadius,
        _ => walkNoiseRadius
    };

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(AimWorldPoint, 0.15f);

        float radius = Application.isPlaying
            ? GetNoiseRadiusForMode(CurrentMode)
            : walkNoiseRadius;
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}