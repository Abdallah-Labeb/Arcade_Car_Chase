using UnityEngine;

/// <summary>
/// Police AI: chases the player, auto-recovers from falls/flips, phases through obstacles.
/// </summary>
public class CarChaseAI : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public CarController carController;

    [Header("Chase Settings")]
    public float catchDistance    = 2.5f;
    public float steerAgression  = 1.2f;
    public float speedMultiplier = 0.92f;

    [Header("Invincibility")]
    public float fallRecoverY = -3f;
    public float maxTiltAngle = 40f;
    public float trackWidth   = 12f;

    private bool playerCaught;
    private Rigidbody rb;
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private float safePositionTimer;

    private void Start()
    {
        if (target == null)
        {
            GameObject t = GameObject.FindGameObjectWithTag("CameraTarget");
            if (t != null) target = t.transform;
        }
        if (carController == null)
            carController = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();

        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;

        IgnoreObstacleCollisions();
    }

    private void IgnoreObstacleCollisions()
    {
        Collider myCol = GetComponent<Collider>();
        if (myCol == null) return;

        GameObject obsParent = GameObject.Find("Obstacles");
        if (obsParent == null) return;

        foreach (var oc in obsParent.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(myCol, oc, true);
    }

    private void Update()
    {
        if (playerCaught || target == null || carController == null) return;

        RecoverIfNeeded();
        SaveSafePosition();
        ClampToTrack();

        float dist = Vector3.Distance(transform.position, target.position);

        // Catch check
        if (dist < catchDistance)
        {
            playerCaught = true;
            carController.GetAIInput(0f, 0f);
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerCaught();
            return;
        }

        // Steering
        Vector3 localTarget = transform.InverseTransformPoint(target.position);
        float steerInput = Mathf.Clamp(localTarget.x / Mathf.Max(Mathf.Abs(localTarget.z), 2f), -1f, 1f);

        // Reverse steering if player is behind
        bool isBehind = localTarget.z < -0.5f;
        if (isBehind) steerInput *= -1f; 

        // Prevent steering off the edge
        if (transform.position.x > trackWidth - 0.5f && steerInput > 0) steerInput = 0;
        if (transform.position.x < -trackWidth + 0.5f && steerInput < 0) steerInput = 0;

        steerInput *= steerAgression;

        // Throttle with catch-up boost and reverse logic
        float throttle = speedMultiplier;
        
        if (isBehind)
        {
            // Reverse or brake hard
            throttle = -1f; 
        }
        else
        {
            if (dist > 10f) throttle *= 1.5f;
            if (dist > 25f) throttle *= 2.5f;
            throttle *= Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(steerInput) * 0.3f);
        }

        carController.GetAIInput(throttle, steerInput);
    }

    private void ClampToTrack()
    {
        bool atEdge = Mathf.Abs(transform.position.x) > trackWidth;
        if (!atEdge) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -trackWidth, trackWidth);
        transform.position = pos;

        if (rb != null)
        {
            // Stronger nudge back to center
            rb.AddForce(Vector3.right * (pos.x > 0 ? -15f : 15f), ForceMode.Acceleration);
            
            // Force rotation back towards center to avoid "sliding" against the edge
            float targetYaw = (pos.x > 0) ? -20f : 20f; // Turn slightly left if on right edge, vice-versa
            Quaternion centerRot = Quaternion.Euler(0, targetYaw, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, centerRot, 5f * Time.deltaTime);
        }
    }

    private void RecoverIfNeeded()
    {
        // Auto-right if tilted
        float tilt = Vector3.Angle(transform.up, Vector3.up);
        if (tilt > maxTiltAngle)
        {
            Quaternion uprightRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, uprightRot, 8f * Time.deltaTime);
            if (rb != null)
                rb.angularVelocity = Vector3.zero;
        }

        // Teleport back if fallen off track
        if (transform.position.y < fallRecoverY)
        {
            transform.position = lastSafePosition + Vector3.up;
            transform.rotation = lastSafeRotation;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void SaveSafePosition()
    {
        safePositionTimer += Time.deltaTime;
        if (safePositionTimer < 0.3f) return;
        safePositionTimer = 0f;

        float tilt = Vector3.Angle(transform.up, Vector3.up);
        if (tilt < 30f && transform.position.y > -1f && Mathf.Abs(transform.position.x) < trackWidth - 1f)
        {
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
        }
    }
}
