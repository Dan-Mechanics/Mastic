using UnityEngine;
using Mirror;

namespace Mastic
{
    public class MouseMovement : NetworkBehaviour
    {
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
            if (!isLocalPlayer)
                return;

            float x = -Input.GetAxisRaw("Mouse Y");
            float y = Input.GetAxisRaw("Mouse X");
            rotation += new Vector2(x, y) * sensitivity;
            rotation.x = Mathf.Clamp(rotation.x, minCamAngle, maxCamAngle);

            SetAsRotation(rotation.x, rotation.y);
            cam.rotation = eyes.rotation;
        }

        public Vector2 GetLocalRotation() => rotation;

        public void SetAsRotation(float xRotation, float yRotation)
        {
            xRotation = Mathf.Clamp(xRotation, minCamAngle, maxCamAngle);
            eyes.localRotation = Quaternion.AngleAxis(xRotation, Vector3.right);
            transform.rotation = Quaternion.AngleAxis(yRotation, Vector3.up);
        }
    }
}