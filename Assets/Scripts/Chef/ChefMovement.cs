using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChefMovement : NetworkBehaviour
{
    //stats
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;

    private Rigidbody rb;
    private float dashTimer;
    private bool isDashing;
    private bool lastIdleState = true;
    private PlayerEmotes playerEmotes;
    private Animator animator;
    private readonly NetworkVariable<bool> isIdle = new NetworkVariable<bool>(true);
    private Vector3 inputDirection;
    private bool dashInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerEmotes = GetComponent<PlayerEmotes>();
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        isIdle.OnValueChanged += OnIdleChanged;
        UpdateIdleAnimation(isIdle.Value);
    }

    public override void OnNetworkDespawn()
    {
        isIdle.OnValueChanged -= OnIdleChanged;
    }

    private void OnIdleChanged(bool previousValue, bool newValue)
    {
        UpdateIdleAnimation(newValue);
    }

    private void UpdateIdleAnimation(bool idle)
    {
        if (animator != null) animator.SetBool("Idle", idle);
    }

    private void Update()
    {
        if (!IsOwner) return;

        inputDirection = Vector3.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputDirection.z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputDirection.z -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDirection.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDirection.x += 1f;

            if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame)
            {
                dashInput = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || !IsOwner) return;

        bool isGamePaused = LevelTimer.Instance == null || !LevelTimer.Instance.isPlaying.Value;
        bool isSelectingEmote = playerEmotes != null && playerEmotes.IsMenuOpen;

        if (isGamePaused || isSelectingEmote)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            isDashing = false;
            dashTimer = 0f;
            UpdateMovementAnimation(true);
            return;
        }

        MovePlayer();
    }

    private void UpdateMovementAnimation(bool idle)
    {
        if (lastIdleState == idle) return;

        lastIdleState = idle;
        UpdateIdleAnimation(idle);

        if (IsServer)
        {
            if (isIdle.Value != idle) isIdle.Value = idle;
        }
        else
        {
            UpdateIdleServerRpc(idle);
        }
    }

    [Rpc(SendTo.Server)]
    private void UpdateIdleServerRpc(bool idle)
    {
        if (isIdle.Value != idle)
            isIdle.Value = idle;
    }

    private void MovePlayer()
    {
        if (dashInput && !isDashing)
        {
            isDashing = true;
            dashTimer = dashDuration;
        }
        dashInput = false; 

        if (inputDirection != Vector3.zero)
        {
            inputDirection.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            Vector3 dashDirection = transform.forward;
            rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, rb.linearVelocity.y, dashDirection.z * dashSpeed);

            if (dashTimer <= 0f)
                isDashing = false;
        }
        else if (inputDirection != Vector3.zero)
        {
            rb.linearVelocity = new Vector3(inputDirection.x * moveSpeed, rb.linearVelocity.y, inputDirection.z * moveSpeed);
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        bool idle = inputDirection == Vector3.zero && !isDashing;
        UpdateMovementAnimation(idle);
    }

    [Rpc(SendTo.Owner)]
    public void ForcePositionRpc(Vector3 newPosition)
    {
        transform.position = newPosition;
        if (rb != null)
        {
            rb.position = newPosition;
            rb.linearVelocity = Vector3.zero;
        }
    }
}