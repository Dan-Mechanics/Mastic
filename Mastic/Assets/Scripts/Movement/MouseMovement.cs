using UnityEngine;

namespace Mastic
{
    public class MouseMovement : MonoBehaviour
    {
        public Vector2 Rotation => rotation;
        public float MinCamAngle => minCamAngle;
        public float MaxCamAngle => maxCamAngle;

        [SerializeField] private Transform eyes = default;
        [SerializeField] private float sensitivity = default;
        [SerializeField] private float minCamAngle = default;
        [SerializeField] private float maxCamAngle = default;
        [SerializeField] private Vector2 rotation = default;
        private Transform cam;

        public void Setup()
        {
            cam = GameObject.FindWithTag("MainCamera").transform;
            if (minCamAngle > maxCamAngle)
                (minCamAngle, maxCamAngle) = (maxCamAngle, minCamAngle);
        }

        private void Update()
        {
            float x = -Input.GetAxisRaw("Mouse Y");
            float y = Input.GetAxisRaw("Mouse X");
            rotation += new Vector2(x, y) * sensitivity;
            rotation.x = Mathf.Clamp(rotation.x, minCamAngle, maxCamAngle);

            SetAsRotation(rotation.x, rotation.y);
            cam.rotation = eyes.rotation;
        }

        /// <summary>
        /// Assuming values have been validated.
        /// </summary>
        public void SetAsRotation(float xRotation, float yRotation)
        {
            eyes.localRotation = Quaternion.AngleAxis(xRotation, Vector3.right);
            transform.rotation = Quaternion.AngleAxis(yRotation, Vector3.up);
        }
    }
}