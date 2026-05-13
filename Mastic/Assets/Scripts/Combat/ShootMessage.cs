using UnityEngine;

namespace Mastic
{
    public struct ShootMessage
    {
        public Vector3 origin;
        public float xRotation;
        public float yRotation;
        public float lerpValue;
        public int movementTick;
        public int rollbackTick;
        public Vector3 debugEnemyPos;

        public void SetValues(Vector3 origin, float xRotation, float yRotation, float lerpValue, int movementTick, int rollbackTick)
        {
            this.origin = origin;
            this.xRotation = xRotation;
            this.yRotation = yRotation;
            this.lerpValue = lerpValue;
            this.movementTick = movementTick;
            this.rollbackTick = rollbackTick;
        }
    }
}