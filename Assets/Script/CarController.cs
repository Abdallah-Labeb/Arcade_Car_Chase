using System.Collections.Generic;
using UnityEngine;

public enum cartype { player, cop }

public class CarController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public GameObject carbody;
    public Transform groundRayPoint;

    [Header("Car Settings")]
    public float forwardSpeed = 25f;
    public float maxSpeed = 45f;
    public float turnSpeed = 280f;

    [Header("Braking / Deceleration")]
    public float deceleration = 0.3f;
    public float coastDrag = 0.5f;
    public float driveDrag = 0.05f;
    public float brakeForce = 12f;

    [Header("Ground Detection")]
    public LayerMask whatIsGround;
    public float groundRayLength = 0.5f;
    [HideInInspector] public bool grounded;

    [Header("Air Control")]
    public float airPitchTorque = 220f;
    public float airRollTorque = 160f;

    [Header("Type")]
    public cartype ct;

    private float vertical;
    private float horizontal;
    private bool braking;
    private Transform[] wheels;
    private bool wasGrounded;

    private void Start()
    {
        if (carbody != null)
        {
            var found = new List<Transform>();
            foreach (Transform child in carbody.GetComponentsInChildren<Transform>())
            {
                if (child.name.StartsWith("wheel"))
                    found.Add(child);
            }
            wheels = found.ToArray();
        }

        if (rb != null)
        {
            rb.linearDamping = driveDrag;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.centerOfMass = new Vector3(0, -0.5f, 0);
        }

        wasGrounded = true;
    }

    private void Update()
    {
        if (ct == cartype.player)
        {
            vertical   = Input.GetAxis("Vertical");
            horizontal = Input.GetAxis("Horizontal");
            braking    = Input.GetKey(KeyCode.Space);
        }

        if (grounded)
            RotateCar();

        SpinWheels();
    }

    public void GetAIInput(float verticalInput, float horizontalInput)
    {
        vertical   = verticalInput;
        horizontal = horizontalInput;
    }

    private void LateUpdate()
    {
        if (carbody == null) return;
        carbody.transform.position = transform.position + new Vector3(0, -0.5f, 0);
        carbody.transform.rotation = transform.rotation;
    }

    private void RotateCar()
    {
        float steer = horizontal * turnSpeed * Time.deltaTime;
        float speedFactor = (ct == cartype.cop)
            ? 1f
            : Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        transform.rotation = Quaternion.Euler(
            transform.eulerAngles + new Vector3(0, steer * speedFactor, 0));
    }

    private void SpinWheels()
    {
        if (wheels == null) return;
        float spinSpeed = rb.linearVelocity.magnitude * 200f * Time.deltaTime;
        foreach (var wheel in wheels)
            wheel.Rotate(spinSpeed, 0, 0);
    }

    private void FixedUpdate()
    {
        grounded = false;
        RaycastHit hit;
        float rayLen = (ct == cartype.cop) ? groundRayLength * 2f : groundRayLength;

        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, rayLen, whatIsGround))
        {
            grounded = true;

            // Only align to ground surface if car isn't flipped (cops always align)
            if (Vector3.Angle(transform.up, Vector3.up) < 60f || ct == cartype.cop)
                transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        }

        // Kill angular velocity on takeoff to prevent unwanted spin
        if (wasGrounded && !grounded)
            rb.angularVelocity = Vector3.zero;

        bool canMove = grounded || (ct == cartype.cop && rb.linearVelocity.y > -3f);

        if (canMove)
        {
            if (braking && ct == cartype.player)
            {
                float speed = rb.linearVelocity.magnitude;
                if (speed > 0.5f)
                    rb.AddForce(-rb.linearVelocity.normalized * brakeForce, ForceMode.Acceleration);
                else
                    rb.linearVelocity = Vector3.zero;
                rb.linearDamping = coastDrag;
            }
            else if (Mathf.Abs(vertical) > 0.01f)
            {
                if (rb.linearVelocity.magnitude < maxSpeed)
                    rb.AddForce(transform.forward * vertical * forwardSpeed);
                rb.linearDamping = driveDrag;
            }
            else
            {
                float brakeFactor = Mathf.Min(deceleration * Time.fixedDeltaTime, 0.3f);
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, brakeFactor);
                rb.linearDamping = coastDrag;
            }
        }
        else
        {
            // Airborne
            rb.linearDamping = 0.01f;

            if (ct == cartype.player)
            {
                if (Mathf.Abs(vertical) > 0.05f)
                    rb.AddTorque(transform.right * vertical * airPitchTorque * Time.fixedDeltaTime, ForceMode.Acceleration);

                if (Mathf.Abs(horizontal) > 0.05f)
                    rb.AddTorque(transform.forward * -horizontal * airRollTorque * Time.fixedDeltaTime, ForceMode.Acceleration);

                // No input → dampen spin
                if (Mathf.Abs(vertical) < 0.05f && Mathf.Abs(horizontal) < 0.05f)
                    rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, 2f * Time.fixedDeltaTime);
            }
            else
            {
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, 5f * Time.fixedDeltaTime);
            }
        }

        wasGrounded = grounded;
    }
}
