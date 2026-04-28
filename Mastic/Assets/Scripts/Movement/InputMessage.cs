using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Represents a tick of player movement input,
    /// including looking direction.
    /// </summary>
    public struct InputMessage
    {
        public bool w;
        public bool a;
        public bool s;
        public bool d;
        // public bool space;

        public float xRotation;
        public float yRotation;
        public int tick;

        public void SetValues(bool w, bool a, bool s, bool d, float xRotation, float yRotation, int tick)
        {
            this.w = w;
            this.a = a;
            this.s = s;
            this.d = d;
            this.xRotation = xRotation;
            this.yRotation = yRotation;
            this.tick = tick;
        }
        
        /// <summary>
        /// Consider adding max look angle 90 degrees here too,
        /// for performance.
        /// </summary>
        public void Verify()
        {
            if (tick < 0)
                tick = 0;
        }

        public float GetVerticalInput()
        {
            float vert = 0f;
            if (w)
                vert++;

            if (s)
                vert--;

            return vert;
        }

        public float GetHorizontalInput()
        {
            float hori = 0f;
            if (d) 
                hori++;

            if (a) 
                hori--;

            return hori;
        }
    }
}