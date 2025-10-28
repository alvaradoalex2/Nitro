using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FlyingEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private int pointValue = 150;
    [SerializeField] private float idleHoverSpeed = 1f;
    [SerializeField] private float idleHoverRange = 0.5f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float chaseRange = 3f;
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private float maxChaseDistance = 10f; // Max distance before returning to start
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat Settings")]
    [SerializeField] private float damageDelay = 0.1f;
    [SerializeField] private float damageCooldown = 0.5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 startPosition;
    private bool isDead = false;
    private float startupTimer = 0f;
    private float damageCooldownTimer = 0f;
    private Transform playerTransform;
    private bool isChasing = false;
    private float hoverTimer = 0f;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsDead = Animator.StringToHash("IsDead");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        startupTimer = 0f;

        // Flying enemies shouldn't be affected by gravity
        rb.gravityScale = 0f;

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
        hoverTimer += Time.deltaTime;

        if (damageCooldownTimer > 0)
        {
            damageCooldownTimer -= Time.deltaTime;
        }

        // Check if player is in chase range
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            float distanceFromStart = Vector2.Distance(transform.position, startPosition);

            // Start chasing if player is close
            if (distanceToPlayer <= chaseRange)
            {
                isChasing = true;
            }
            // Stop chasing if too far from start position
            else if (distanceFromStart > maxChaseDistance)
            {
                isChasing = false;
            }
        }

        // Move enemy
        if (isChasing && playerTransform != null)
        {
            ChasePlayer();
        }
        else
        {
            ReturnToStart();
        }

        UpdateAnimation();
    }

    private void ReturnToStart()
    {
        float distanceFromStart = Vector2.Distance(transform.position, startPosition);

        // If close to start position, just hover
        if (distanceFromStart < 0.1f)
        {
            IdleHover();
        }
        else
        {
            // Move back to start position
            Vector2 directionToStart = (startPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = directionToStart * returnSpeed;

            // Face movement direction
            if (directionToStart.x > 0.1f && transform.localScale.x < 0)
            {
                Flip(1);
            }
            else if (directionToStart.x < -0.1f && transform.localScale.x > 0)
            {
                Flip(-1);
            }
        }
    }

    private void IdleHover()
    {
        // Gentle hovering motion (figure-8 or sine wave)
        float hoverOffset = Mathf.Sin(hoverTimer * idleHoverSpeed) * idleHoverRange;
        Vector2 targetPosition = startPosition + new Vector2(0, hoverOffset);

        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * (idleHoverSpeed * 0.5f);
    }

    private void ChasePlayer()
    {
        // Move towards player
        Vector2 directionToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = directionToPlayer * chaseSpeed;

        // Face the player
        if (directionToPlayer.x > 0.1f && transform.localScale.x < 0)
        {
            Flip(1);
        }
        else if (directionToPlayer.x < -0.1f && transform.localScale.x > 0)
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
            damageCooldownTimer = damageCooldown;
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

        animator.SetFloat(Speed, rb.linearVelocity.magnitude);
        animator.SetBool(IsDead, isDead);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw chase range
        Gizmos.color = Color.yellow;
        Vector2 pos = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.DrawWireSphere(pos, chaseRange);

        // Draw max chase distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, maxChaseDistance);

        // Draw idle hover range
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos + Vector2.up * idleHoverRange, pos + Vector2.down * idleHoverRange);

        // Draw line to player when chasing
        if (Application.isPlaying && isChasing && playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}