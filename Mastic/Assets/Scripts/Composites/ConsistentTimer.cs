using UnityEngine;

namespace Mastic
{
    public struct ConsistentTimer
    {
        public float interval;
        public float value;
        public int maxConsecutiveTicks;

        public ConsistentTimer(float interval, float value = 0f, int maxConsecutiveTicks = 0)
        {
            this.interval = interval;
            this.value = value;
            this.maxConsecutiveTicks = maxConsecutiveTicks;
        }

        public int Tick(float dt)
        {
            int ticks = 0;
            value += dt;
            if (maxConsecutiveTicks > 0)
                value = Mathf.Clamp(value, 0f, interval * maxConsecutiveTicks);

            while (value >= interval)
            {
                value -= interval;
                ticks++;
            }

            return ticks;
        }
    }
}
