using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private int pointValue = 100;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private bool canPatrol = true;

    [Header("Chase Settings")]
    [SerializeField] private bool canChase = true;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float chaseRange = 6f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat Settings")]
    [SerializeField] private float damageDelay = 0.1f; // Reduced delay
    [SerializeField] private float damageCooldown = 0.5f; // Cooldown between hits

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 startPosition;
    private int moveDirection = 1;
    private bool isDead = false;
    private float startupTimer = 0f;
    private float damageCooldownTimer = 0f;
    private Transform playerTransform;
    private bool isChasing = false;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsDead = Animator.StringToHash("IsDead");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        startupTimer = 0f;

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Update timers
        startupTimer += Time.deltaTime;
        if (damageCooldownTimer > 0)
        {
            damageCooldownTimer -= Time.deltaTime;
        }

        // Check if player is in chase range
        if (canChase && playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            isChasing = distanceToPlayer <= chaseRange;
        }

        // Move enemy
        if (isChasing)
        {
            ChasePlayer();
        }
        else if (canPatrol)
        {
            Patrol();
        }

        UpdateAnimation();
    }

    private void Patrol()
    {
        // Move in current direction
        rb.linearVelocity = new Vector2(moveDirection * patrolSpeed, rb.linearVelocity.y);

        // Check if we've reached patrol distance from start position
        float distanceFromStart = transform.position.x - startPosition.x;

        // If we've moved too far in either direction, flip
        if (distanceFromStart >= patrolDistance)
        {
            moveDirection = -1;
            Flip(-1);
        }
        else if (distanceFromStart <= -patrolDistance)
        {
            moveDirection = 1;
            Flip(1);
        }
    }

    private void ChasePlayer()
    {
        // Move towards player
        float directionToPlayer = Mathf.Sign(playerTransform.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(directionToPlayer * chaseSpeed, rb.linearVelocity.y);

        // Face the player
        if (directionToPlayer > 0 && transform.localScale.x < 0)
        {
            Flip(1);
        }
        else if (directionToPlayer < 0 && transform.localScale.x > 0)
        {
            Flip(-1);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Only damage after startup delay and if cooldown has passed
        if (!isDead &&
            other.CompareTag("Player") &&
            startupTimer >= damageDelay &&
            damageCooldownTimer <= 0)
        {
            GameManager.Instance?.PlayerHit();
            damageCooldownTimer = damageCooldown; // Start cooldown
        }
    }

    public void TakeDamage()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;

        // Award points
        GameManager.Instance?.AddScore(pointValue);

        // Play death sound
        AudioManager.Instance?.PlaySound("EnemyDeath");

        // Destroy after short delay
        Destroy(gameObject, 0.5f);
    }

    private void Flip(int direction)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetFloat(Speed, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(IsDead, isDead);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw damage radius (not used anymore but kept for reference)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw patrol range
        if (canPatrol)
        {
            Vector2 pos = Application.isPlaying ? startPosition : (Vector2)transform.position;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos + Vector2.left * patrolDistance, pos + Vector2.right * patrolDistance);
        }

        // Draw chase range
        if (canChase)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }
    }
}