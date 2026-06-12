using Mirror;
using UnityEngine;

namespace Mastic
{
    public class MouseLook : NetworkBehaviour
    {
        public float RotationX => rotation.x;
        public float RotationY => rotation.y;

        [Tooltip("Use this to define the spawn rotation.")]
        [SerializeField] private Vector2 rotation = default;
        private float sensitivity = 1f;
        private Transform eyes;
        private Transform cam;

        private void Awake()
        {
            cam = GameObject.FindWithTag("MainCamera").transform;
            eyes = transform.Find("eyes");
        }

        public void SetSensitivity(float sensitivity)
        {
            if (sensitivity > 0f)
                this.sensitivity = sensitivity;
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            float x = -Input.GetAxisRaw("Mouse Y");
            float y = Input.GetAxisRaw("Mouse X");
            rotation += new Vector2(x, y) * sensitivity;
            SetRotationDirectly(rotation.x, rotation.y);
        }

        public void SetRotationDirectly(float xRotation, float yRotation)
        {
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            rotation.x = xRotation;
            rotation.y = yRotation;

            SetRotationTemporarily(xRotation, yRotation);
            cam.rotation = eyes.rotation;
        }

        public void SetRotationTemporarily(float xRotation, float yRotation)
        {
            eyes.localRotation = Quaternion.AngleAxis(xRotation, Vector3.right);
            transform.rotation = Quaternion.AngleAxis(yRotation, Vector3.up);
        }
    }
}