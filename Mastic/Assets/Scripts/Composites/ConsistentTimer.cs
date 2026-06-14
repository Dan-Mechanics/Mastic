using System;
using UnityEngine;

namespace Mastic
{
    public struct ConsistentTimer
    {
        public float interval;
        public float value;
        public float maxConsecutiveTicks;

        public ConsistentTimer(float interval, float value = 0f, float maxConsecutiveTicks = -1)
        {
            this.interval = interval;
            this.value = value;
            this.maxConsecutiveTicks = maxConsecutiveTicks;
        }

        public int Tick(float dt)
        {
            int result = 0;

            value += dt;
            if (maxConsecutiveTicks > 0f)
                value = Mathf.Clamp(value, 0f, interval * maxConsecutiveTicks);

            while (value >= interval)
            {
                value -= interval;
                result++;
            }

            return result;
        }
    }
}
