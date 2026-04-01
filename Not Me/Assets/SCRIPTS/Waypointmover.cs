using UnityEngine;

/// <summary>
/// WaypointMover — Attach this script to any GameObject (e.g. a car).
/// Set Point A and Point B in the Inspector, then press Play.
/// </summary>
public class WaypointMover : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Starting position (drag an empty GameObject here)")]
    public Transform pointA;

    [Tooltip("Destination position (drag an empty GameObject here)")]
    public Transform pointB;

    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    public float speed = 5f;

    [Tooltip("If true, the object will loop back and forth between A and B")]
    public bool pingPong = true;

    [Tooltip("How close the object must be to a waypoint before switching targets (meters)")]
    public float arrivalThreshold = 0.1f;

    [Header("Rotation Settings")]
    [Tooltip("Smoothly rotate the object to face its movement direction")]
    public bool faceDirection = true;

    [Tooltip("How fast the object rotates to face its target (degrees/sec)")]
    public float rotationSpeed = 5f;

    // ── private state ──────────────────────────────────────────────────
    private Transform _currentTarget;
    private bool      _movingToB = true;
    private bool      _arrived   = false;

    // ──────────────────────────────────────────────────────────────────
    void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogError("[WaypointMover] Please assign both Point A and Point B in the Inspector!", this);
            enabled = false;
            return;
        }

        // Snap the object to Point A at the start
        transform.position = pointA.position;
        _currentTarget     = pointB;
        _movingToB         = true;
    }

    // ──────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_arrived && !pingPong) return;   // Stopped at destination

        MoveTowardsTarget();
        CheckArrival();
    }

    // ──────────────────────────────────────────────────────────────────
    private void MoveTowardsTarget()
    {
        Vector3 direction = (_currentTarget.position - transform.position).normalized;

        // Translate
        transform.position = Vector3.MoveTowards(
            transform.position,
            _currentTarget.position,
            speed * Time.deltaTime
        );

        // Optional rotation
        if (faceDirection && direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // ──────────────────────────────────────────────────────────────────
    private void CheckArrival()
    {
        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);

        if (distanceToTarget <= arrivalThreshold)
        {
            if (pingPong)
            {
                // Flip direction
                _movingToB    = !_movingToB;
                _currentTarget = _movingToB ? pointB : pointA;
            }
            else
            {
                // Stop at destination
                transform.position = _currentTarget.position;
                _arrived           = true;
                Debug.Log("[WaypointMover] Arrived at destination.");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    /// <summary>Call this at runtime to restart the journey from Point A.</summary>
    public void RestartJourney()
    {
        transform.position = pointA.position;
        _currentTarget     = pointB;
        _movingToB         = true;
        _arrived           = false;
    }

    // ── Editor Gizmos (visible in Scene view) ─────────────────────────
    void OnDrawGizmos()
    {
        if (pointA == null || pointB == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(pointA.position, 0.3f);
        Gizmos.DrawLine(pointA.position, pointB.position);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(pointB.position, 0.3f);
    }
}