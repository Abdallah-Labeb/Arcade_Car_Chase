using UnityEngine;

/// <summary>
/// One-time speed reduction + upward bump when car enters the trigger.
/// </summary>
public class SpeedBumpTrigger : MonoBehaviour
{
    public float slowFactor = 0.85f;
    public float bumpForce = 3f;

    private void OnTriggerEnter(Collider other)
    {
        // Skip cop cars — only affect the player
        CarController cc = other.GetComponent<CarController>();
        if (cc != null && cc.ct == cartype.cop) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0, v.z);
        if (horizontal.magnitude > 0.5f)
            rb.linearVelocity = horizontal * slowFactor + Vector3.up * (v.y + bumpForce);
    }
}
