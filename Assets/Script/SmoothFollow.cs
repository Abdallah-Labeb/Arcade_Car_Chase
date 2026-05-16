using UnityEngine;

namespace UnityStandardAssets.Utility
{
    /// <summary>
    /// Advanced smooth follow camera.
    /// - Smooth follow
    /// - Stable during flips
    /// - Speed-based FOV
    /// - Camera collision prevention
    /// - Dynamic tilt
    /// - Reduced jitter
    /// </summary>
    public class SmoothFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Distance")]
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 10f;

        [Header("Height")]
        [SerializeField] private float height = 3f;

        [Header("Smoothing")]
        [SerializeField] private float rotationDamping = 5f;
        [SerializeField] private float positionDamping = 8f;

        [Header("Look")]
        [SerializeField] private float lookDownOffset = 1.5f;

        [Header("Speed Effects")]
        [SerializeField] private float distanceSpeedEffect = 0.05f;
        [SerializeField] private float fovSpeedEffect = 0.4f;
        [SerializeField] private float minFov = 60f;
        [SerializeField] private float maxFov = 85f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private float collisionOffset = 0.3f;

        [Header("Tilt Protection")]
        [SerializeField] private float maxTiltAngle = 65f;

        private Transform carSphereTransform;

        private Rigidbody targetRb;
        private Camera cam;
        private CarController cachedCarController;

        private float lastValidYaw;

        private Vector3 currentVelocity;

        private void Start()
        {
            GameObject targetObj =
                GameObject.FindGameObjectWithTag("CameraTarget");

            if (targetObj == null)
                return;

            carSphereTransform = targetObj.transform;

            Transform sedanChild =
                targetObj.transform.parent
                ? targetObj.transform.parent.Find("sedan")
                : null;

            Transform camTarget =
                sedanChild
                ? sedanChild.Find("camTarget")
                : null;

            target =
                camTarget != null
                ? camTarget
                : carSphereTransform;

            lastValidYaw =
                carSphereTransform.eulerAngles.y;

            targetRb =
                carSphereTransform.GetComponent<Rigidbody>();

            cachedCarController =
                carSphereTransform.GetComponent<CarController>();

            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null || carSphereTransform == null)
                return;

            UpdateYaw();
            UpdateCamera();
            UpdateFOV();
        }

        // =====================================================
        // YAW STABILIZATION
        // =====================================================

        private void UpdateYaw()
        {
            float tilt =
                Vector3.Angle(
                    carSphereTransform.up,
                    Vector3.up
                );

            // Ignore flips
            if (tilt < maxTiltAngle)
            {
                lastValidYaw =
                    Mathf.LerpAngle(
                        lastValidYaw,
                        carSphereTransform.eulerAngles.y,
                        rotationDamping * Time.deltaTime
                    );
            }
        }

        // =====================================================
        // CAMERA MOVEMENT
        // =====================================================

        private void UpdateCamera()
        {
            float speed = 0f;

            if (targetRb != null)
                speed = targetRb.linearVelocity.magnitude;

            // Dynamic distance
            float dynamicDistance =
                Mathf.Lerp(
                    minDistance,
                    maxDistance,
                    speed * distanceSpeedEffect / maxSpeedReference
                );

            dynamicDistance =
                Mathf.Clamp(
                    dynamicDistance,
                    minDistance,
                    maxDistance
                );

            // Rotation
            Quaternion rotation =
                Quaternion.Euler(0f, lastValidYaw, 0f);

            // Desired position
            Vector3 desiredPosition =
                target.position
                - rotation * Vector3.forward * dynamicDistance
                + Vector3.up * height;

            // Collision prevention
            desiredPosition =
                HandleCameraCollision(
                    target.position,
                    desiredPosition
                );

            // Smooth movement
            transform.position =
                Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref currentVelocity,
                    1f / positionDamping
                );

            // Look target
            Vector3 lookPosition =
                target.position
                + Vector3.up * lookDownOffset;

            Quaternion lookRotation =
                Quaternion.LookRotation(
                    lookPosition - transform.position
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    lookRotation,
                    rotationDamping * Time.deltaTime
                );

            // Remove unwanted roll
            Vector3 euler = transform.eulerAngles;

            transform.rotation =
                Quaternion.Euler(
                    euler.x,
                    euler.y,
                    0f
                );
        }

        // =====================================================
        // CAMERA COLLISION
        // =====================================================

        private Vector3 HandleCameraCollision(
            Vector3 from,
            Vector3 to
        )
        {
            Vector3 dir = to - from;
            float distanceToCam = dir.magnitude;

            if (
                Physics.Raycast(
                    from,
                    dir.normalized,
                    out RaycastHit hit,
                    distanceToCam,
                    collisionMask
                )
            )
            {
                return hit.point
                    + hit.normal * collisionOffset;
            }

            return to;
        }

        // =====================================================
        // SPEED FOV
        // =====================================================

        private void UpdateFOV()
        {
            if (cam == null || targetRb == null)
                return;

            float speed =
                targetRb.linearVelocity.magnitude;

            float targetFov =
                Mathf.Lerp(
                    minFov,
                    maxFov,
                    speed * fovSpeedEffect / maxSpeedReference
                );

            cam.fieldOfView =
                Mathf.Lerp(
                    cam.fieldOfView,
                    targetFov,
                    3f * Time.deltaTime
                );
        }

        // =====================================================
        // SPEED REFERENCE
        // =====================================================

        private float maxSpeedReference
        {
            get
            {
                if (cachedCarController != null)
                    return cachedCarController.maxSpeed;

                return 50f;
            }
        }
    }
}