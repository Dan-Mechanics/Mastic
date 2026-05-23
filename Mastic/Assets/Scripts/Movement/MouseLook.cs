using UnityEngine;
using Mirror;

namespace Mastic
{
    public class MouseLook : NetworkBehaviour
    {
        public float RotationX => rotation.x;
        public float RotationY => rotation.y;
        
        [SerializeField] private float sensitivity = default;
        [SerializeField] private float minCamAngle = default;
        [SerializeField] private float maxCamAngle = default;
        [SerializeField] private Vector2 rotation = default;
        private Transform eyes;
        private Transform cam;

        private void Awake()
        {
            cam = GameObject.FindWithTag("MainCamera").transform;
            eyes = transform.Find("eyes");
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
            SetRotation(rotation.x, rotation.y);
        }

        public void SetRotation(float xRotation, float yRotation)
        {
            xRotation = Mathf.Clamp(xRotation, minCamAngle, maxCamAngle);
            rotation.x = xRotation;
            rotation.y = yRotation;

            SetRotationTransient(xRotation, yRotation);
            cam.rotation = eyes.rotation;
        }

        public void SetRotationTransient(float xRotation, float yRotation)
        {
            eyes.localRotation = Quaternion.AngleAxis(xRotation, Vector3.right);
            transform.rotation = Quaternion.AngleAxis(yRotation, Vector3.up);
        }
    }
}