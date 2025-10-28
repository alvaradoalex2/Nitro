using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;

    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private float minX = -100f;
    [SerializeField] private float maxX = 100f;
    [SerializeField] private float minY = -100f;
    [SerializeField] private float maxY = 100f;

    [Header("Look Ahead")]
    [SerializeField] private bool useLookAhead = false;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 2f;

    private Vector3 velocity = Vector3.zero;
    private float currentLookAhead = 0f;

    private void Start()
    {
        if (autoFindPlayer && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = CalculateDesiredPosition();

        // Smooth damp for smooth following
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothSpeed
        );

        // Apply bounds if enabled
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        transform.position = smoothedPosition;
    }

    private Vector3 CalculateDesiredPosition()
    {
        Vector3 targetPosition = target.position + offset;

        // Look ahead in the direction the player is moving
        if (useLookAhead)
        {
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                float targetLookAhead = Mathf.Sign(targetRb.linearVelocity.x) * lookAheadDistance;
                currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, Time.deltaTime * lookAheadSpeed);
                targetPosition.x += currentLookAhead;
            }
        }

        // Apply follow axis settings
        Vector3 desiredPosition = transform.position;
        if (followX) desiredPosition.x = targetPosition.x;
        if (followY) desiredPosition.y = targetPosition.y;
        desiredPosition.z = targetPosition.z;

        return desiredPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;

            // Draw bounds rectangle
            Vector3 topLeft = new Vector3(minX, maxY, 0);
            Vector3 topRight = new Vector3(maxX, maxY, 0);
            Vector3 bottomLeft = new Vector3(minX, minY, 0);
            Vector3 bottomRight = new Vector3(maxX, minY, 0);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }

        // Draw camera view bounds
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            Gizmos.color = Color.cyan;
            float height = 2f * cam.orthographicSize;
            float width = height * cam.aspect;

            Vector3 pos = transform.position;
            Gizmos.DrawWireCube(new Vector3(pos.x, pos.y, 0), new Vector3(width, height, 0));
        }
    }
}