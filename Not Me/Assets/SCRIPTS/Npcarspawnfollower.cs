using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPCCarSpawnFollower
/// ─────────────────────────────────────────────────────────────────
/// Attach this script to your animated human NPC GameObject.
///
/// HOW IT WORKS:
///   1. The NPC starts HIDDEN at the spawn point.
///   2. When the trigger car enters the SpawnTriggerRadius around
///      the spawn point it plays a short "exit car" delay, then
///      reveals the NPC and it begins following the VR camera rig.
///   3. The NPC always turns to face the player smoothly and keeps
///      a comfortable follow distance using Unity's NavMeshAgent.
///
/// SETUP CHECKLIST:
///   □ Add a NavMeshAgent component to this GameObject.
///   □ Bake a NavMesh in your scene (Window > AI > Navigation).
///   □ Assign the public fields in the Inspector (see below).
///   □ Make sure your Animator has parameters: "IsWalking" (bool)
///     and optionally "ExitCar" (trigger) for a get-out animation.
/// ─────────────────────────────────────────────────────────────────
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NPCCarSpawnFollower : MonoBehaviour
{
    // ─── Inspector Fields ────────────────────────────────────────

    [Header("References")]
    [Tooltip("The VR Camera Rig (or XR Origin) the NPC will follow.")]
    public Transform cameraRig;

    [Tooltip("The car GameObject that triggers the NPC spawn.")]
    public Transform triggerCar;

    [Header("Spawn Settings")]
    [Tooltip("World position where the NPC will appear (place a GameObject here as a marker).")]
    public Transform spawnPoint;

    [Tooltip("How close the car must get to the spawn point before the NPC appears (metres).")]
    public float spawnTriggerRadius = 3f;

    [Tooltip("Seconds after the car arrives before the NPC actually appears (simulates getting out).")]
    public float exitCarDelay = 1.8f;

    [Header("Follow Settings")]
    [Tooltip("How close the NPC gets before it stops walking towards the player.")]
    public float stoppingDistance = 1.8f;

    [Tooltip("How fast the NPC walks (NavMeshAgent speed).")]
    public float walkSpeed = 1.4f;

    [Tooltip("How quickly the NPC rotates to face the player (degrees per second).")]
    public float rotationSpeed = 120f;

    [Header("Animator Parameters")]
    [Tooltip("Bool parameter name in the Animator that controls the walk cycle.")]
    public string walkingParam = "IsWalking";

    [Tooltip("Trigger parameter name in the Animator for the 'exit car' animation. Leave blank to skip.")]
    public string exitCarTriggerParam = "ExitCar";

    // ─── Private State ───────────────────────────────────────────

    private NavMeshAgent _agent;
    private Animator     _animator;
    private bool         _spawned          = false;
    private bool         _spawnInProgress  = false;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        // Hide NPC at the start
        SetNPCVisible(false);

        // Snap to spawn point position immediately (hidden)
        if (spawnPoint != null)
            transform.position = spawnPoint.position;
    }

    void Update()
    {
        // ── Phase 1: Waiting for the car ────────────────────────
        if (!_spawned && !_spawnInProgress)
        {
            if (triggerCar == null || spawnPoint == null) return;

            float distanceCarToSpawn = Vector3.Distance(triggerCar.position, spawnPoint.position);

            if (distanceCarToSpawn <= spawnTriggerRadius)
            {
                _spawnInProgress = true;
                StartCoroutine(SpawnSequence());
            }
            return;
        }

        // ── Phase 2: Following the player ───────────────────────
        if (_spawned && cameraRig != null)
        {
            FollowPlayer();
        }
    }

    // ─── Spawn Sequence ──────────────────────────────────────────

    private IEnumerator SpawnSequence()
    {
        // Wait for the "exit car" delay
        yield return new WaitForSeconds(exitCarDelay);

        // Show NPC at spawn point
        transform.position = spawnPoint != null ? spawnPoint.position : transform.position;
        SetNPCVisible(true);

        // Play exit-car animation if configured
        if (!string.IsNullOrEmpty(exitCarTriggerParam))
        {
            _animator.SetTrigger(exitCarTriggerParam);

            // Give the animation a moment before the NPC starts walking
            yield return new WaitForSeconds(1.0f);
        }

        // Configure NavMeshAgent
        _agent.speed            = walkSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.angularSpeed     = 0f; // We handle rotation manually for smoothness

        _spawned         = true;
        _spawnInProgress = false;
    }

    // ─── Follow Logic ────────────────────────────────────────────

    private void FollowPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, cameraRig.position);
        bool  shouldWalk       = distanceToPlayer > stoppingDistance;

        // Update NavMesh destination every frame
        _agent.SetDestination(cameraRig.position);

        // Control walking animation
        _animator.SetBool(walkingParam, shouldWalk);

        // Smooth rotation toward player (only on Y axis to keep NPC upright)
        Vector3 directionToPlayer = cameraRig.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private void SetNPCVisible(bool visible)
    {
        // Toggle all renderers on this GameObject and its children
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;

        // Disable agent movement while hidden
        _agent.enabled = visible;
    }

    // ─── Editor Gizmos ───────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (spawnPoint == null) return;

        // Draw the car trigger radius around the spawn point
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(spawnPoint.position, spawnTriggerRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawSphere(spawnPoint.position, spawnTriggerRadius);

        // Draw follow stopping distance around the NPC
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}