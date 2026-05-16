using System.Collections.Generic;
using UnityEngine;

public enum cartype
{
    player,
    cop
}

public class CarController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public GameObject carbody;
    public Transform groundRayPoint;

    [Header("Movement")]
    public float acceleration = 30f;
    public float maxSpeed = 45f;
    public float reverseSpeed = 15f;

    [Header("Steering")]
    public float turnSpeed = 220f;
    public float steeringResponsiveness = 6f;

    [Tooltip("How much steering is reduced at max speed (0 = no reduction, 1 = fully locked)")]
    [Range(0f, 0.9f)]
    public float highSpeedSteerReduction = 0.55f;

    [Header("Grip")]
    public float sideGrip = 4f;
    public float driftGrip = 2f;

    [Tooltip("Extra grip added at max speed to prevent sliding out on turns")]
    public float highSpeedGripBoost = 6f;

    [Tooltip("Downforce applied at max speed to keep car planted")]
    public float downforce = 5f;

    [Header("Drag")]
    public float driveDrag = 0.05f;
    public float coastDrag = 1.2f;

    [Header("Brakes")]
    public float brakeForce = 20f;

    [Header("Ground Detection")]
    public LayerMask whatIsGround;
    public float groundRayLength = 0.8f;

    [Header("Ground Alignment")]
    public float alignSpeed = 3f;
    public float maxGroundAngle = 60f;

    [Header("Air Control")]
    public float airPitchTorque = 220f;
    public float airRollTorque = 160f;
    public float airStability = 3f;

    [Header("Type")]
    public cartype ct;

    [HideInInspector]
    public bool grounded;

    private float vertical;
    private float horizontal;
    private bool braking;

    private float smoothSteer;

    private Transform[] wheels;

    private bool wasGrounded;

    private void Start()
    {
        // Find wheels automatically
        if (carbody != null)
        {
            List<Transform> found = new List<Transform>();

            foreach (Transform child in carbody.GetComponentsInChildren<Transform>())
            {
                if (child.name.ToLower().StartsWith("wheel"))
                {
                    found.Add(child);
                }
            }

            wheels = found.ToArray();
        }

        if (rb != null)
        {
            rb.linearDamping = driveDrag;
            rb.angularDamping = 1f;

            // IMPORTANT FOR SMOOTHNESS
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            // Lower center of mass slightly
            rb.centerOfMass = new Vector3(0, -0.35f, 0);
        }

        wasGrounded = true;
    }

    private void Update()
    {
        // Player input
        if (ct == cartype.player)
        {
            vertical = Input.GetAxisRaw("Vertical");
            horizontal = Input.GetAxisRaw("Horizontal");

            braking = Input.GetKey(KeyCode.Space);
        }

        SpinWheels();
    }

    public void GetAIInput(float verticalInput, float horizontalInput)
    {
        vertical = Mathf.Clamp(verticalInput, -1f, 1f);

        horizontal = Mathf.Clamp(horizontalInput, -1f, 1f);
    }

    private void FixedUpdate()
    {
        CheckGround();

        if (grounded)
        {
            AlignToGround();

            ApplyMovement();

            ApplySteering();

            ApplySideGrip();
        }
        else
        {
            ApplyAirControl();
        }

        wasGrounded = grounded;
    }

    private void LateUpdate()
    {
        UpdateBodyVisual();
    }

    // =====================================================
    // GROUND CHECK
    // =====================================================

    private void CheckGround()
    {
        grounded = false;

        float rayLength =
            ct == cartype.cop
            ? groundRayLength * 1.5f
            : groundRayLength;

        if (
            Physics.Raycast(
                groundRayPoint.position,
                -transform.up,
                out RaycastHit hit,
                rayLength,
                whatIsGround
            )
        )
        {
            grounded = true;

            // Reduce spinning after landing
            if (!wasGrounded)
            {
                rb.angularVelocity *= 0.3f;
            }
        }
    }

    // =====================================================
    // MOVEMENT
    // =====================================================

    private void ApplyMovement()
    {
        float forwardSpeed =
            Vector3.Dot(
                rb.linearVelocity,
                transform.forward
            );

        bool reversing = vertical < 0f;

        float speedLimit =
            reversing
            ? reverseSpeed
            : maxSpeed;

        // Acceleration
        if (Mathf.Abs(forwardSpeed) < speedLimit)
        {
            rb.AddForce(
                transform.forward *
                vertical *
                acceleration,
                ForceMode.Acceleration
            );
        }

        // Braking
        if (braking && ct == cartype.player)
        {
            rb.AddForce(
                -rb.linearVelocity *
                brakeForce *
                Time.fixedDeltaTime,
                ForceMode.Acceleration
            );

            rb.linearDamping = coastDrag * 2f;
        }
        else
        {
            rb.linearDamping =
                Mathf.Abs(vertical) > 0.05f
                ? driveDrag
                : coastDrag;
        }

        // Stop tiny shaking movement
        if (
            Mathf.Abs(vertical) < 0.05f &&
            rb.linearVelocity.magnitude < 0.15f
        )
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    // =====================================================
    // STEERING
    // =====================================================

    private void ApplySteering()
    {
        float speedRatio =
            Mathf.Clamp01(
                rb.linearVelocity.magnitude / maxSpeed
            );

        // At low speed: need some speed to turn (ramps up quickly).
        // At high speed: reduce steering to prevent twitchy over-steer.
        // Curve peaks around 30-50% speed, then falls off.
        float lowSpeedRamp = Mathf.Clamp01(speedRatio * 3f);
        float highSpeedFalloff = 1f - speedRatio * highSpeedSteerReduction;
        float steerCurve = lowSpeedRamp * highSpeedFalloff;

        // Steering input smoothing — more smoothing at high speed
        // so the car doesn't snap on small corrections
        float adaptiveResponsiveness =
            Mathf.Lerp(
                steeringResponsiveness,
                steeringResponsiveness * 0.4f,
                speedRatio
            );

        smoothSteer = Mathf.Lerp(
            smoothSteer,
            horizontal,
            adaptiveResponsiveness *
            Time.fixedDeltaTime
        );

        float steerAmount =
            smoothSteer *
            turnSpeed *
            steerCurve *
            Time.fixedDeltaTime;

        Quaternion turnRotation =
            Quaternion.Euler(0f, steerAmount, 0f);

        rb.MoveRotation(
            rb.rotation * turnRotation
        );
    }

    // =====================================================
    // SIDE GRIP
    // =====================================================

    private void ApplySideGrip()
    {
        Vector3 localVelocity =
            transform.InverseTransformDirection(
                rb.linearVelocity
            );

        float speedRatio =
            Mathf.Clamp01(
                rb.linearVelocity.magnitude / maxSpeed
            );

        // Base grip + extra grip at high speed to prevent sliding out
        float baseGrip =
            braking
            ? driftGrip
            : sideGrip;

        float grip = baseGrip +
            highSpeedGripBoost * speedRatio;

        // Remove sideways sliding smoothly
        localVelocity.x =
            Mathf.Lerp(
                localVelocity.x,
                0f,
                grip * Time.fixedDeltaTime
            );

        rb.linearVelocity =
            transform.TransformDirection(localVelocity);

        // Downforce — keeps car planted at speed
        if (downforce > 0f && ct == cartype.player)
        {
            rb.AddForce(
                -transform.up *
                downforce *
                speedRatio,
                ForceMode.Acceleration
            );
        }
    }

    // =====================================================
    // GROUND ALIGNMENT
    // =====================================================

    private void AlignToGround()
    {
        if (
            Physics.Raycast(
                groundRayPoint.position,
                -transform.up,
                out RaycastHit hit,
                groundRayLength * 1.5f,
                whatIsGround
            )
        )
        {
            float groundAngle =
                Vector3.Angle(
                    hit.normal,
                    Vector3.up
                );

            // Ignore walls
            if (groundAngle <= maxGroundAngle || ct == cartype.cop)
            {
                // IMPORTANT:
                // Prevent tiny constant corrections
                float groundDifference =
                    Vector3.Angle(
                        transform.up,
                        hit.normal
                    );

                if (groundDifference > 1f)
                {
                    Quaternion targetRotation =
                        Quaternion.FromToRotation(
                            transform.up,
                            hit.normal
                        ) * rb.rotation;

                    rb.MoveRotation(
                        Quaternion.Slerp(
                            rb.rotation,
                            targetRotation,
                            alignSpeed *
                            Time.fixedDeltaTime
                        )
                    );
                }
            }
        }
    }

    // =====================================================
    // AIR CONTROL
    // =====================================================

    private void ApplyAirControl()
    {
        rb.linearDamping = 0.02f;

        if (ct == cartype.player)
        {
            if (Mathf.Abs(vertical) > 0.05f)
            {
                rb.AddTorque(
                    transform.right *
                    vertical *
                    airPitchTorque *
                    Time.fixedDeltaTime,
                    ForceMode.Acceleration
                );
            }

            if (Mathf.Abs(horizontal) > 0.05f)
            {
                rb.AddTorque(
                    transform.forward *
                    -horizontal *
                    airRollTorque *
                    Time.fixedDeltaTime,
                    ForceMode.Acceleration
                );
            }
        }

        // Stabilize in air
        rb.angularVelocity =
            Vector3.Lerp(
                rb.angularVelocity,
                Vector3.zero,
                airStability *
                Time.fixedDeltaTime
            );
    }

    // =====================================================
    // VISUAL BODY
    // =====================================================

    private void UpdateBodyVisual()
    {
        if (carbody == null)
            return;

        // Smooth visual movement
        carbody.transform.position =
            Vector3.Lerp(
                carbody.transform.position,
                transform.position + Vector3.down * 0.5f,
                12f * Time.deltaTime
            );

        // Smooth visual rotation
        carbody.transform.rotation =
            Quaternion.Slerp(
                carbody.transform.rotation,
                transform.rotation,
                12f * Time.deltaTime
            );
    }

    // =====================================================
    // WHEELS
    // =====================================================

    private void SpinWheels()
    {
        if (wheels == null)
            return;

        float speed =
            rb.linearVelocity.magnitude *
            250f *
            Time.deltaTime;

        foreach (Transform wheel in wheels)
        {
            wheel.Rotate(speed, 0f, 0f);
        }
    }
}