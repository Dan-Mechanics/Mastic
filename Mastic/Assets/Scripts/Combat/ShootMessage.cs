using UnityEngine;

namespace Mastic
{
    public struct ShootMessage
    {
        public float xRotation;
        public float yRotation;
        public float lerpValue;
        public int movementTick;
        public int rollbackTick;
        public Vector3 origin;

        public void SetRotation(float xRotation, float yRotation)
        {
            this.xRotation = xRotation;
            this.yRotation = yRotation;
        }

        public void SetTicks(int movementTick, int rollbackTick)
        {
            this.movementTick = movementTick;
            this.rollbackTick = rollbackTick;
        }

        public void SetPosition(Vector3 origin, float lerpValue)
        {
            this.origin = origin;
            this.lerpValue = lerpValue;
        }
    }
}