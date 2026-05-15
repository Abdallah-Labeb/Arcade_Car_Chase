using UnityEngine;

/// <summary>
/// Detects if the player car has flipped on the ground or fallen off the track.
/// Airborne flips are allowed — only grounded flips trigger game over.
/// </summary>
public class FlipDetector : MonoBehaviour
{
    public float flipAngleThreshold = 70f;
    public float flipTimeThreshold = 2f;
    public float fallThreshold = -15f;

    private float flipTimer;
    private bool triggered;
    private CarController carController;

    private void Start()
    {
        carController = GetComponentInParent<CarController>();
        if (carController == null)
            carController = GetComponent<CarController>();

        if (carController == null)
            Debug.LogWarning($"[FlipDetector] No CarController found on '{gameObject.name}' or its parents. " +
                             "Ground-flip detection will be disabled (only fall detection will work).");
    }

    private void Update()
    {
        if (triggered) return;

        if (transform.position.y < fallThreshold)
        {
            TriggerCrash();
            return;
        }

        bool isGrounded = carController != null && carController.grounded;
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        if (tiltAngle > flipAngleThreshold && isGrounded)
        {
            flipTimer += Time.deltaTime;
            if (flipTimer >= flipTimeThreshold)
                TriggerCrash();
        }
        else
        {
            flipTimer = 0f;
        }
    }

    private void TriggerCrash()
    {
        triggered = true;
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerFlipped();
    }
}
