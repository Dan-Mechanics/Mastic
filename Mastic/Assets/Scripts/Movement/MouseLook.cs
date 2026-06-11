using UnityEngine;
using Mirror;
using System;

namespace Mastic
{
    public class MouseLook : NetworkBehaviour
    {
        public float RotationX => rotation.x;
        public float RotationY => rotation.y;
        
        [SerializeField] private EasyVar sensitivity = default;
        [SerializeField] private float minCamAngle = default;
        [SerializeField] private float maxCamAngle = default;
        [SerializeField] private Vector2 rotation = default;
        private Transform eyes;
        private Transform cam;
        private float sens;

        private void Awake()
        {
            cam = GameObject.FindWithTag("MainCamera").transform;
            eyes = transform.Find("eyes");
            if (minCamAngle > maxCamAngle)
                (minCamAngle, maxCamAngle) = (maxCamAngle, minCamAngle);
        }

        private void Start()
        {
            try
            {
                sens = sensitivity.Get<float>();
            }
            catch (Exception exception)
            {
                sens = EasySettings.Current.Get<float>(nameof(sens));
                Debug.LogWarning(exception.Message);
            }
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            float x = -Input.GetAxisRaw("Mouse Y");
            float y = Input.GetAxisRaw("Mouse X");
            rotation += new Vector2(x, y) * sens;
            SetRotationDirectly(rotation.x, rotation.y);
        }

        public void SetRotationDirectly(float xRotation, float yRotation)
        {
            xRotation = Mathf.Clamp(xRotation, minCamAngle, maxCamAngle);
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