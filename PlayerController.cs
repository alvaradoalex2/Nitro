using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 50f;
    [SerializeField] private float airControl = 0.5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float coyoteTime = 0.1f; // Grace period after leaving ground

    [Header("Wall Detection")]
    [SerializeField] private Transform wallCheckPoint;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private int maxDashCharges = 1;
    [SerializeField] private float dashInvincibilityDuration = 0.1f; // Invincible for first half of dash
    [SerializeField] private bool dashDamagesEnemies = true;

    [Header("Combat Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackDuration = 0.15f;
    [SerializeField] private float attackHitboxActiveTime = 0.05f; // How long the hitbox is active

    // Components
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Animator animator;

    // Input
    private Vector2 moveInput;
    private bool spacePressed;

    // State
    private bool isGrounded;
    private bool wasTrulyGrounded; // Actually standing on ground (not just near it)
    private bool isDashing;
    private bool isAttacking;
    private int dashCharges;
    private float dashTimer;
    private float dashCooldownTimer;
    private float attackTimer;
    private int facingDirection = 1; // 1 = right, -1 = left
    private Vector2 dashDirection; // Store dash direction
    private bool isDashInvincible;
    private bool isTouchingWall;
    private float coyoteTimer;

    // Animation hashes (for performance)
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int IsDashing = Animator.StringToHash("IsDashing");

    // Public properties
    public bool IsDashInvincible => isDashInvincible;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        dashCharges = maxDashCharges;
    }

    private void Update()
    {
        CheckGroundStatus();
        CheckWallStatus();
        HandleTimers();
        HandleCoyoteTime();
        HandleSpaceBarInput();

        // Check for enemy hits during dash
        if (isDashing && dashDamagesEnemies)
        {
            CheckDashEnemyHits();
        }

        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            HandleDashMovement();
        }
        else if (!isAttacking)
        {
            HandleMovement();
        }
        else
        {
            // During attack: LOCK player in place completely
            rb.linearVelocity = Vector2.zero;
        }

        ApplyBetterJump();
    }

    private void CheckGroundStatus()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Check if truly grounded (for jump vs dash distinction)
        // Use a tighter check to ensure we're actually on the ground
        wasTrulyGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius * 0.5f, groundLayer);

        // Reset dash charges on landing
        if (!wasGrounded && isGrounded)
        {
            dashCharges = maxDashCharges;
            coyoteTimer = coyoteTime;
            OnLanded();
        }

        // Start coyote timer when leaving ground
        if (wasGrounded && !isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
    }

    private void CheckWallStatus()
    {
        bool wasTouchingWall = isTouchingWall;

        // Check for wall in facing direction
        Vector2 wallCheckDirection = new Vector2(facingDirection, 0);
        RaycastHit2D wallHit = Physics2D.Raycast(
            wallCheckPoint != null ? wallCheckPoint.position : transform.position,
            wallCheckDirection,
            wallCheckDistance,
            wallLayer
        );

        isTouchingWall = wallHit.collider != null;

        // Reset dash when touching wall and holding direction into it
        if (isTouchingWall && !isGrounded)
        {
            // Check if player is holding direction into the wall
            bool holdingIntoWall = (facingDirection > 0 && moveInput.x > 0.1f) ||
                                   (facingDirection < 0 && moveInput.x < -0.1f);

            if (holdingIntoWall)
            {
                // End current dash if dashing into wall
                if (isDashing)
                {
                    isDashing = false;
                    isDashInvincible = false;
                    dashTimer = 0;
                    Debug.Log("Dash cancelled by wall contact");
                }

                // Reset dash charges
                if (!wasTouchingWall || dashCharges < maxDashCharges)
                {
                    dashCharges = maxDashCharges;
                    Debug.Log("Wall grab - Dash reset!");
                }

                // Reduce vertical velocity for wall sliding effect
                if (rb.linearVelocity.y < 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
                }
            }
        }
    }

    private void HandleCoyoteTime()
    {
        if (coyoteTimer > 0 && !isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    private void HandleSpaceBarInput()
    {
        if (!spacePressed) return;

        // Priority 1: Attack if grounded and near enemy (MUST BE FIRST - prevents jump)
        if (wasTrulyGrounded && !isAttacking && !isDashing && IsNearEnemy())
        {
            StartAttack();
            spacePressed = false; // Consume input immediately
            return; // Exit early to prevent any other actions
        }
        // Priority 2: Jump if truly grounded OR within coyote time
        else if ((wasTrulyGrounded || coyoteTimer > 0) && !isAttacking && !isDashing)
        {
            Jump();
            coyoteTimer = 0; // Consume coyote time
        }
        // Priority 3: Dash if in air (includes wall grab state)
        else if (!wasTrulyGrounded && coyoteTimer <= 0 && dashCharges > 0 && dashCooldownTimer <= 0 && !isAttacking)
        {
            StartDash();
        }

        spacePressed = false;
    }

    private bool IsNearEnemy()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        return hitEnemies.Length > 0;
    }

    private void HandleMovement()
    {
        float targetSpeed = moveInput.x * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // Apply air control modifier
        if (!isGrounded)
        {
            accelRate *= airControl;
        }

        float movement = speedDiff * accelRate;
        rb.AddForce(movement * Vector2.right);

        // Flip sprite based on movement direction
        if (moveInput.x > 0.1f && facingDirection == -1)
        {
            Flip();
        }
        else if (moveInput.x < -0.1f && facingDirection == 1)
        {
            Flip();
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        AudioManager.Instance?.PlaySound("Jump");
    }

    private void ApplyBetterJump()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !spacePressed)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashCharges--;
        isDashInvincible = true;

        // Determine dash direction based on input
        Vector2 inputDirection = moveInput.normalized;

        // If no directional input, dash in facing direction
        if (inputDirection.magnitude < 0.1f)
        {
            dashDirection = new Vector2(facingDirection, 0);
        }
        else
        {
            // Use input direction for dash
            dashDirection = inputDirection.normalized;

            // Update facing direction based on horizontal input
            if (inputDirection.x > 0.1f)
            {
                facingDirection = 1;
                if (transform.localScale.x < 0) Flip();
            }
            else if (inputDirection.x < -0.1f)
            {
                facingDirection = -1;
                if (transform.localScale.x > 0) Flip();
            }
        }

        // Apply dash velocity
        rb.linearVelocity = dashDirection * dashSpeed;

        AudioManager.Instance?.PlaySound("Dash");
    }

    private void HandleDashMovement()
    {
        // Maintain dash velocity in the dash direction
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    private void CheckDashEnemyHits()
    {
        // Determine attack point position based on dash direction
        Vector2 attackPosition = GetDashAttackPosition();

        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Check for regular Enemy
            if (enemy.TryGetComponent<Enemy>(out Enemy enemyComponent))
            {
                enemyComponent.TakeDamage();

                // Reset dash charge on successful hit
                dashCharges = maxDashCharges;

                Debug.Log("Enemy hit during dash!");
            }
            // Check for FlyingEnemy
            else if (enemy.TryGetComponent<FlyingEnemy>(out FlyingEnemy flyingEnemyComponent))
            {
                flyingEnemyComponent.TakeDamage();

                // Reset dash charge on successful hit
                dashCharges = maxDashCharges;

                Debug.Log("Flying Enemy hit during dash!");
            }
        }
    }

    private Vector2 GetDashAttackPosition()
    {
        Vector2 basePosition = transform.position;

        // Position attack point based on dash direction
        if (Mathf.Abs(dashDirection.y) > 0.7f) // Mostly vertical dash
        {
            if (dashDirection.y > 0) // Dashing up
            {
                return basePosition + new Vector2(0, attackRange);
            }
            else // Dashing down
            {
                return basePosition + new Vector2(0, -attackRange);
            }
        }
        else // Horizontal or diagonal dash
        {
            float xOffset = dashDirection.x > 0 ? attackRange : -attackRange;
            return basePosition + new Vector2(xOffset, 0);
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackDuration;

        // IMMEDIATELY stop ALL movement to keep player completely grounded
        rb.linearVelocity = Vector2.zero;

        // Detect enemies in range immediately (instant hitbox)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Enemy>(out Enemy enemyComponent))
            {
                enemyComponent.TakeDamage();

                // Reset dash charge on successful hit
                dashCharges = maxDashCharges;

                Debug.Log("Enemy Hit!"); // Visual feedback
            }
        }

        AudioManager.Instance?.PlaySound("Attack");
        Debug.Log("Attack Started!"); // Debug to see when attack fires
    }

    private void HandleTimers()
    {
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;

            // Remove invincibility halfway through dash
            if (dashTimer <= dashDuration * 0.5f && isDashInvincible)
            {
                isDashInvincible = false;
            }

            if (dashTimer <= 0)
            {
                isDashing = false;
                isDashInvincible = false;
            }
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                isAttacking = false;
            }
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat(Speed, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(IsGrounded, isGrounded);
        animator.SetBool(IsAttacking, isAttacking);
        animator.SetBool(IsDashing, isDashing);
    }

    private void Flip()
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnLanded()
    {
        AudioManager.Instance?.PlaySound("Land");
    }

    // Input System callbacks
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            spacePressed = true;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            spacePressed = true;
        }
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed)
        {
            spacePressed = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

            // Draw tighter ground check for "truly grounded"
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius * 0.5f);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

        // Draw wall check
        if (wallCheckPoint != null || Application.isPlaying)
        {
            Vector3 startPos = wallCheckPoint != null ? wallCheckPoint.position : transform.position;
            Gizmos.color = isTouchingWall ? Color.magenta : Color.blue;
            Gizmos.DrawLine(startPos, startPos + new Vector3(facingDirection * wallCheckDistance, 0, 0));
        }

        // Draw dash attack positions when dashing (in play mode)
        if (Application.isPlaying && isDashing)
        {
            Gizmos.color = Color.cyan;
            Vector2 dashAttackPos = GetDashAttackPosition();
            Gizmos.DrawWireSphere(dashAttackPos, attackRange);
        }
    }
}