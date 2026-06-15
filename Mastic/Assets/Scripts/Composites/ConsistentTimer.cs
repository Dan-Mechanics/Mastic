namespace Mastic
{
    /// <summary>
    /// Note: this may be broken.
    /// </summary>
    public struct ConsistentTimer
    {
        public float interval;
        public float value;

        public ConsistentTimer(float interval, float value = 0f)
        {
            this.interval = interval;
            this.value = value;
        }

        public int Tick(float dt)
        {
            int result = 0;

            value += dt;
            while (value >= interval)
            {
                value -= interval;
                result++;
            }

            return result;
        }
    }
}
