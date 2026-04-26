using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Represents a tick of player movement input.
    /// </summary>
    public struct InputMessage
    {
        public bool forward;
        public bool left;
        public bool backward;
        public bool right;
        //public bool space;

        public float yRotation;
        public float xRotation;
        public int tick;

        public void SetMovement(bool forward, bool backward, bool left, bool right)
        {
            this.forward = forward;
            this.backward = backward;
            this.left = left;
            this.right = right;
        }

        public void SetTick(int tick) => this.tick = tick;

        public void SetRotation(float yRotation, float xRotation)
        {
            this.yRotation = yRotation;
            this.xRotation = Mathf.Clamp(xRotation, -MouseMovement.MAX_CAM_ANGLE, MouseMovement.MAX_CAM_ANGLE);
        }

        public float GetVerticalInput()
        {
            float forwardInput = 0f;
            if (forward)
                forwardInput++;

            if (backward)
                forwardInput--;

            return forwardInput;
        }

        public float GetHorizontalInput()
        {
            float sideways = 0f;
            if (right) 
                sideways++;

            if (left) 
                sideways--;

            return sideways;
        }

        public Quaternion GetHorizontalRotation() => Quaternion.AngleAxis(yRotation, Vector3.up);
        public Quaternion GetVerticalRotation() => Quaternion.AngleAxis(xRotation, Vector3.right);
    }
}