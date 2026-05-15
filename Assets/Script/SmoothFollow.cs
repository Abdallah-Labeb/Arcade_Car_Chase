using UnityEngine;

namespace UnityStandardAssets.Utility
{
    public class SmoothFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 10.0f;
        [SerializeField] private float height   = 5.0f;
        [SerializeField] private float rotationDamping = 3f;
        [SerializeField] private float heightDamping   = 2f;

        private Transform carSphereTransform;
        private float lastValidYaw;

        private void Start()
        {
            GameObject targetObj = GameObject.FindGameObjectWithTag("CameraTarget");
            if (targetObj == null) return;

            carSphereTransform = targetObj.transform;

            Transform sedanChild = targetObj.transform.parent
                ? targetObj.transform.parent.Find("sedan")
                : null;
            Transform camTarget = sedanChild ? sedanChild.Find("camTarget") : null;

            target = camTarget != null ? camTarget : carSphereTransform;
            lastValidYaw = carSphereTransform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (!target || !carSphereTransform) return;

            // Only update yaw when car is mostly upright (ignore flips)
            float tilt = Vector3.Angle(carSphereTransform.up, Vector3.up);
            if (tilt < 60f)
                lastValidYaw = carSphereTransform.eulerAngles.y;

            float wantedHeight    = target.position.y + height;
            float currentRotAngle = Mathf.LerpAngle(transform.eulerAngles.y, lastValidYaw, rotationDamping * Time.deltaTime);
            float currentHeight   = Mathf.Lerp(transform.position.y, wantedHeight, heightDamping * Time.deltaTime);

            var currentRotation = Quaternion.Euler(0, currentRotAngle, 0);

            transform.position = target.position - currentRotation * Vector3.forward * distance;
            transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);

            Vector3 lookPos = target.position;
            lookPos.y = transform.position.y - 2f;
            transform.LookAt(lookPos);
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }
    }
}