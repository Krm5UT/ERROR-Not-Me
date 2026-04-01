using UnityEngine;

/// <summary>
/// WaypointMover — Attach to a car GameObject.
///
/// AUDIO SETUP:
///   1. Add TWO AudioSource components to this GameObject.
///   2. Assign one to "Driving Audio Source" and one to "Arrival Audio Source".
///   3. Assign your audio clips in the fields below.
///   4. Both AudioSources will be auto-configured for VR spatial audio.
///
/// WHY TWO AUDIOSOURCES?
///   Using two separate AudioSources lets the engine loop stop cleanly and
///   independently while the arrival sound plays — no bleed-over between them.
/// </summary>
public class WaypointMover : MonoBehaviour
{
    // ── Waypoints ──────────────────────────────────────────────────────
    [Header("Waypoints")]
    [Tooltip("Starting position (drag an empty GameObject here)")]
    public Transform pointA;

    [Tooltip("Destination position (drag an empty GameObject here)")]
    public Transform pointB;

    // ── Movement ───────────────────────────────────────────────────────
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    public float speed = 5f;

    [Tooltip("If true, the object loops back and forth between A and B")]
    public bool pingPong = true;

    [Tooltip("How close the object must be before counting as arrived (meters)")]
    public float arrivalThreshold = 0.1f;

    // ── Rotation ───────────────────────────────────────────────────────
    [Header("Rotation Settings")]
    [Tooltip("Smoothly rotate to face movement direction")]
    public bool faceDirection = true;

    [Tooltip("How fast the object rotates (degrees/sec)")]
    public float rotationSpeed = 5f;

    // ── Audio ──────────────────────────────────────────────────────────
    [Header("Audio Sources")]
    [Tooltip("AudioSource dedicated to the engine loop.\nAdd a second AudioSource component and drag it here.")]
    public AudioSource drivingAudioSource;

    [Tooltip("AudioSource dedicated to the arrival sound.\nAdd a third AudioSource component and drag it here.")]
    public AudioSource arrivalAudioSource;

    [Header("Audio Clips")]
    [Tooltip("Short looping engine sound — Unity will repeat it automatically")]
    public AudioClip drivingClip;

    [Tooltip("One-shot sound played when the car reaches Point B (e.g. brakes, parking)")]
    public AudioClip arrivalClip;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float drivingVolume = 1f;

    [Range(0f, 1f)]
    public float arrivalVolume = 1f;

    [Tooltip("Seconds to fade the engine in/out (0 = instant)")]
    public float fadeDuration = 0.5f;

    [Tooltip("Distance (meters) at which the engine sound is at full volume")]
    public float audioMinDistance = 1f;

    [Tooltip("Distance (meters) at which the engine sound becomes inaudible")]
    public float audioMaxDistance = 30f;

    // ── Private state ──────────────────────────────────────────────────
    private Transform _currentTarget;
    private bool      _movingToB     = true;
    private bool      _arrived       = false;
    private float     _currentVolume = 0f;
    private bool      _fadingIn      = false;
    private bool      _fadingOut     = false;

    // ──────────────────────────────────────────────────────────────────
    void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogError("[WaypointMover] Assign Point A and Point B in the Inspector!", this);
            enabled = false;
            return;
        }

        SetupAudioSource(drivingAudioSource,  loop: true);
        SetupAudioSource(arrivalAudioSource,  loop: false);

        transform.position = pointA.position;
        _currentTarget     = pointB;
        _movingToB         = true;

        StartDrivingAudio();
    }

    // ──────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_arrived && !pingPong)
        {
            HandleFade();
            return;
        }

        MoveTowardsTarget();
        CheckArrival();
        HandleFade();
    }

    // ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// Configures an AudioSource for full 3D spatial audio.
    /// spatialBlend = 1 makes it positional (essential for VR).
    /// rolloffMode = Logarithmic matches real-world sound falloff.
    /// </summary>
    private void SetupAudioSource(AudioSource source, bool loop)
    {
        if (source == null) return;

        source.loop           = loop;
        source.playOnAwake    = false;
        source.volume         = 0f;
        source.spatialBlend   = 1f;                              // Full 3D — required for VR
        source.spatialize     = true;                            // Enables VR SDK HRTF processing
        source.rolloffMode    = AudioRolloffMode.Logarithmic;    // Realistic distance falloff
        source.minDistance    = audioMinDistance;
        source.maxDistance    = audioMaxDistance;
        source.dopplerLevel   = 0.5f;                            // Subtle doppler as car passes
    }

    // ──────────────────────────────────────────────────────────────────
    private void MoveTowardsTarget()
    {
        Vector3 direction = (_currentTarget.position - transform.position).normalized;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _currentTarget.position,
            speed * Time.deltaTime
        );

        if (faceDirection && direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // ──────────────────────────────────────────────────────────────────
    private void CheckArrival()
    {
        if (Vector3.Distance(transform.position, _currentTarget.position) > arrivalThreshold)
            return;

        if (pingPong)
        {
            _movingToB     = !_movingToB;
            _currentTarget = _movingToB ? pointB : pointA;
        }
        else
        {
            transform.position = _currentTarget.position;
            _arrived           = true;

            StopDrivingAudio();   // Fades out and STOPS the engine loop
            PlayArrivalAudio();   // Plays on its own dedicated AudioSource
            Debug.Log("[WaypointMover] Arrived at Point B.");
        }
    }

    // ── Audio ──────────────────────────────────────────────────────────

    private void StartDrivingAudio()
    {
        if (drivingAudioSource == null || drivingClip == null) return;

        drivingAudioSource.clip   = drivingClip;
        drivingAudioSource.volume = 0f;
        drivingAudioSource.Play();

        _fadingIn  = true;
        _fadingOut = false;
    }

    private void StopDrivingAudio()
    {
        if (drivingAudioSource == null) return;

        _fadingIn  = false;
        _fadingOut = true;   // Will call audioSource.Stop() once fully faded
    }

    private void PlayArrivalAudio()
    {
        if (arrivalAudioSource == null || arrivalClip == null) return;

        // Completely separate AudioSource — engine loop stopping won't affect this
        arrivalAudioSource.clip   = arrivalClip;
        arrivalAudioSource.volume = arrivalVolume;
        arrivalAudioSource.Play();
    }

    private void HandleFade()
    {
        if (drivingAudioSource == null) return;

        if (_fadingIn)
        {
            float step = fadeDuration > 0f
                ? (drivingVolume / fadeDuration) * Time.deltaTime
                : drivingVolume;

            _currentVolume = Mathf.MoveTowards(_currentVolume, drivingVolume, step);
            drivingAudioSource.volume = _currentVolume;

            if (Mathf.Approximately(_currentVolume, drivingVolume))
                _fadingIn = false;
        }
        else if (_fadingOut)
        {
            float step = fadeDuration > 0f
                ? (drivingVolume / fadeDuration) * Time.deltaTime
                : _currentVolume;

            _currentVolume = Mathf.MoveTowards(_currentVolume, 0f, step);
            drivingAudioSource.volume = _currentVolume;

            if (Mathf.Approximately(_currentVolume, 0f))
            {
                _fadingOut = false;
                drivingAudioSource.Stop();   // Fully stopped — no more looping
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    public void RestartJourney()
    {
        transform.position = pointA.position;
        _currentTarget     = pointB;
        _movingToB         = true;
        _arrived           = false;

        StartDrivingAudio();
    }

    // ── Gizmos ────────────────────────────────────────────────────────
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
