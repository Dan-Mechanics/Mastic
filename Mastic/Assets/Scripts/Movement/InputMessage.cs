using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Represents a tick of player movement input.
    /// </summary>
    public struct InputMessage
    {
        public bool w;
        public bool a;
        public bool s;
        public bool d;
        //public bool space;

        public float yRotation;
        public float xRotation;
        public int tick;

        public void SetValues(bool w, bool a, bool s, bool d, float yRotation, float xRotation, int tick)
        {
            this.w = w;
            this.a = a;
            this.s = s;
            this.d = d;
            this.yRotation = yRotation;
            this.xRotation = xRotation;
            this.tick = tick;
        }
        
        public void Verify() => xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        public float GetVerticalInput()
        {
            float vert = 0f;
            if (w)
                vert++;

            if (a)
                vert--;

            return vert;
        }

        public float GetHorizontalInput()
        {
            float hori = 0f;
            if (d) 
                hori++;

            if (s) 
                hori--;

            return hori;
        }

        public Quaternion GetHorizontalRotation() => Quaternion.AngleAxis(yRotation, Vector3.up);
        public Quaternion GetVerticalRotation() => Quaternion.AngleAxis(xRotation, Vector3.right);
    }
}