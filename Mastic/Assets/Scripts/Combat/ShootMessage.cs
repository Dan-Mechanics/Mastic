namespace Mastic
{
    public struct ShootMessage
    {
        public float xRotation;
        public float yRotation;
        public float lerpValue;
        public int rollbackTick;
        public int movementTick;

        public ShootMessage(float xRotation, float yRotation, float lerpValue, int rollbackTick, int movementTick)
        {
            this.xRotation = xRotation;
            this.yRotation = yRotation;
            this.lerpValue = lerpValue;
            this.rollbackTick = rollbackTick;
            this.movementTick = movementTick;
        }
    }
}