using UnityEngine;

/// <summary>
/// Stable police chase AI.
/// - Chases player
/// - Recovers from flips/falls
/// - Avoids endless reversing
/// - Stays inside track
/// - Ignores obstacle collisions
/// </summary>
public class CarChaseAI : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public CarController carController;

    [Header("Chase Settings")]
    public float catchDistance = 3f;
    public float steerAggression = 1.5f;
    public float baseThrottle = 0.8f;

    [Header("Track Settings")]
    public float trackWidth = 12f;
    public float edgeForce = 20f;

    [Header("Recovery Settings")]
    public float fallRecoverY = -3f;
    public float maxTiltAngle = 45f;
    public float uprightSpeed = 6f;

    private Rigidbody rb;

    private bool playerCaught;

    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;

    private float safeTimer;
    private float currentSteer;

    private void Start()
    {
        if (target == null)
        {
            GameObject t = GameObject.FindGameObjectWithTag("CameraTarget");

            if (t != null)
                target = t.transform;
        }

        if (carController == null)
            carController = GetComponent<CarController>();

        rb = GetComponent<Rigidbody>();

        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;

        IgnoreObstacleCollisions();
    }

    private void FixedUpdate()
    {
        if (playerCaught || target == null || carController == null)
            return;

        RecoverIfNeeded();
        SaveSafePosition();
        ClampToTrack();

        HandleChaseLogic();
    }

    private void HandleChaseLogic()
    {
        float dist = Vector3.Distance(transform.position, target.position);

        // Catch player
        if (dist < catchDistance)
        {
            playerCaught = true;

            carController.GetAIInput(0f, 0f);

            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerCaught();

            return;
        }

        Vector3 localTarget =
            transform.InverseTransformPoint(target.position);

        // Steering calculation
        float targetSteer =
            Mathf.Clamp(
                localTarget.x /
                Mathf.Max(Mathf.Abs(localTarget.z), 2f),
                -1f,
                1f
            );

        bool targetBehind = localTarget.z < 0f;

        // If target behind:
        // rotate aggressively instead of reversing forever
        if (targetBehind)
        {
            targetSteer *= 2f;
        }

        targetSteer *= steerAggression;

        // Smooth steering
        currentSteer = Mathf.Lerp(
            currentSteer,
            targetSteer,
            5f * Time.fixedDeltaTime
        );

        // Prevent steering outside track
        if (transform.position.x > trackWidth - 0.5f && currentSteer > 0)
            currentSteer = 0;

        if (transform.position.x < -trackWidth + 0.5f && currentSteer < 0)
            currentSteer = 0;

        // Throttle
        float throttle = baseThrottle;

        // Catch-up boost (exclusive tiers, not stacking)
        if (dist > 25f)
            throttle *= 1.7f;
        else if (dist > 10f)
            throttle *= 1.3f;

        // Slow down during sharp turns
        throttle *= Mathf.Lerp(
            0.7f,
            1f,
            1f - Mathf.Abs(currentSteer) * 0.4f
        );

        // If target behind:
        // slow forward movement while turning
        if (targetBehind)
        {
            throttle = 0.35f;
        }

        carController.GetAIInput(throttle, currentSteer);
    }

    private void ClampToTrack()
    {
        if (Mathf.Abs(transform.position.x) <= trackWidth)
            return;

        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(
            pos.x,
            -trackWidth,
            trackWidth
        );

        transform.position = pos;

        if (rb != null)
        {
            // Push car back toward center
            float pushDirection = pos.x > 0 ? -1f : 1f;

            rb.AddForce(
                Vector3.right * pushDirection * edgeForce,
                ForceMode.Acceleration
            );

            // Slightly rotate toward center
            Vector3 centerDir =
                new Vector3(pushDirection, 0f, 1f).normalized;

            Quaternion targetRot =
                Quaternion.LookRotation(centerDir);

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetRot,
                    3f * Time.fixedDeltaTime
                )
            );
        }
    }

    private void RecoverIfNeeded()
    {
        float tilt =
            Vector3.Angle(transform.up, Vector3.up);

        // Auto upright
        if (tilt > maxTiltAngle)
        {
            Quaternion uprightRotation =
                Quaternion.Euler(
                    0f,
                    transform.eulerAngles.y,
                    0f
                );

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    uprightRotation,
                    uprightSpeed * Time.fixedDeltaTime
                )
            );

            rb.angularVelocity = Vector3.zero;
        }

        // Fell off map
        if (transform.position.y < fallRecoverY)
        {
            transform.position =
                lastSafePosition + Vector3.up * 2f;

            transform.rotation = lastSafeRotation;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SaveSafePosition()
    {
        safeTimer += Time.fixedDeltaTime;

        if (safeTimer < 0.3f)
            return;

        safeTimer = 0f;

        float tilt =
            Vector3.Angle(transform.up, Vector3.up);

        bool safe =
            tilt < 30f &&
            transform.position.y > -1f &&
            Mathf.Abs(transform.position.x) < trackWidth - 1f;

        if (safe)
        {
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
        }
    }

    private void IgnoreObstacleCollisions()
    {
        GameObject obstacleParent =
            GameObject.Find("Obstacles");

        if (obstacleParent == null)
            return;

        Collider[] myColliders =
            GetComponentsInChildren<Collider>();

        Collider[] obstacleColliders =
            obstacleParent.GetComponentsInChildren<Collider>();

        foreach (Collider myCol in myColliders)
        {
            foreach (Collider obstacleCol in obstacleColliders)
            {
                Physics.IgnoreCollision(
                    myCol,
                    obstacleCol,
                    true
                );
            }
        }
    }
}