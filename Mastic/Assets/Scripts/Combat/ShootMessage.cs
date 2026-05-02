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
        public Vector3 debugEyesPos;
        public Vector3 debugEnemyPos;

        public void SetValues(float xRotation, float yRotation, float lerpValue, int movementTick, int rollbackTick)
        {
            this.xRotation = xRotation;
            this.yRotation = yRotation;
            this.lerpValue = lerpValue;
            this.movementTick = movementTick;
            this.rollbackTick = rollbackTick;
        }

        public void SetDebugFields(Vector3 debugEyesPos, Vector3 debugEnemyPos)
        {
            this.debugEyesPos = debugEyesPos;
            this.debugEnemyPos = debugEnemyPos;
        }
    }
}