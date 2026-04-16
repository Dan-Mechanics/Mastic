using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class MouseMovement : MonoBehaviour
    {
        [SerializeField] private Transform eyes = null;
        [SerializeField] private float sensitivity = 0.33f;

        [SerializeField] private bool useMainCamera = true;

        private Transform cam;

        //private const float MIN_CAM_ANGLE = -90f;
        public const float MAX_CAM_ANGLE = 90f;
        
        public Vector2 rotation;

        private Vector2 mouseMovement;
        private float mouseInputX;
        private float mouseInputY;

        private void Awake()
        {
            if (useMainCamera) { cam = GameObject.FindWithTag("MainCamera").transform; }
        }

        private void Update()
        {
            //if (Input.GetKey(KeyCode.Mouse0) && Input.GetKey(KeyCode.Mouse1)) { return; }

            //if (Input.GetKey(KeyCode.Mouse1)) { return; }

            mouseInputX = Input.GetAxisRaw("Mouse X");
            mouseInputY = -Input.GetAxisRaw("Mouse Y");

            // i am flipping these because left and right is Y arrow direction.
            //mouseMovement = new Vector2(mouseInputY, mouseInputX + (Input.GetKey(KeyCode.Mouse1) ? 180f * Time.deltaTime : 0f));
            mouseMovement = new Vector2(mouseInputY, mouseInputX);

            /*if (Input.GetKey(KeyCode.Mouse1)) { mouseMovement.y += 180f * Time.deltaTime; }
            if (Input.GetKey(KeyCode.Mouse0)) { mouseMovement.y -= 180f * Time.deltaTime; }*/

            rotation += mouseMovement * sensitivity;

            rotation.x = Mathf.Clamp(rotation.x, -MAX_CAM_ANGLE, MAX_CAM_ANGLE);

            eyes.localRotation = Quaternion.AngleAxis(rotation.x, Vector3.right);

            // this could be local rotation if u are planning on giving player parent object but i dont plan to
            transform.rotation = Quaternion.AngleAxis(rotation.y, Vector3.up);

            if (useMainCamera) { cam.rotation = eyes.rotation; }
        }
    }
}