using UnityEngine;

namespace Mastic
{
    public class NetcodeIdea : MonoBehaviour
    {
        // EXAMPLE STUFF HERE.
        public Transform cam;
        public int tickrate;
        public MouseLook mouseLook;
        public ICameraInterpolation interpolation;
        public PlayerEntity playerEntity;

        private InputPayload inputPayload;

        private void Start()
        {
            Time.fixedDeltaTime = 1f / tickrate;
        }

        private void Update()
        {
            if (inputPayload.mouse0)
                return;

            if (Input.GetKey(KeyCode.Mouse0))
            {
                inputPayload.mouse0 = true;
                inputPayload.shootRotX = mouseLook.RotationX;
                inputPayload.shootRotY = mouseLook.RotationY;
                inputPayload.lerpValue = interpolation.LerpValue;
                inputPayload.unlocalTick = playerEntity.DisplayedTick;
                // etc etc for the shoot tick, sub tick accuracy
            }
        }

        private void FixedUpdate()
        {
            inputPayload.w = Input.GetKey(KeyCode.W);
            inputPayload.a = Input.GetKey(KeyCode.A);
            inputPayload.s = Input.GetKey(KeyCode.S);
            inputPayload.d = Input.GetKey(KeyCode.D);
            inputPayload.rotX = mouseLook.RotationX;
            inputPayload.rotY = mouseLook.RotationY;
            inputPayload.r = Input.GetKey(KeyCode.R);

            // USE THE STUFF HERE !!
            // ...

            // PREPARE THE NEXT ITERATION.
            inputPayload.mouse0 = false;
        }

        public struct InputPayload
        {
            public int tick;

            // MOVE.
            public bool w, a, s, d, space;
            public float rotX;
            public float rotY;

            // SHOOT.
            public bool r, mouse0;
            public float shootRotX;
            public float shootRotY;
            public float lerpValue;
            public int unlocalTick;
            // public float unlocalLerpValue;
        }
    }
}
