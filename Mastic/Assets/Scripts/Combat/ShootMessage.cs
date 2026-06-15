using UnityEngine;

namespace Mastic
{
    public struct ShootMessage
    {
        public Vector3 origin;
        public float xRotation;
        public float yRotation;
        public float localLerpValue;
        public float unlocalLerpValue;
        public int inputTick;
        public bool isCleanSlate;
        public int rollbackTick;

        /// <summary>
        /// Remove this when testing is done.
        /// </summary>
        public Vector3 debugEnemyPos;

        public void SetValues(Vector3 origin, float xRotation, float yRotation, bool isCleanSlate, float lerpValue, float unlocalLerpValue, int inputTick, int rollbackTick)
        {
            this.origin = origin;
            this.xRotation = xRotation;
            this.isCleanSlate = isCleanSlate;
            this.yRotation = yRotation;
            this.localLerpValue = lerpValue;
            this.unlocalLerpValue = unlocalLerpValue;
            this.inputTick = inputTick;
            this.rollbackTick = rollbackTick;
        }
    }
}