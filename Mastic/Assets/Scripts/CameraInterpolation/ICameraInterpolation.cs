using UnityEngine;

namespace Mastic
{
    public interface ICameraInterpolation 
    {
        public float LerpValue { get; set; }
        public bool IsInterjected { get; set; }
        void Assign(Vector3 pos, Vector3 vel);
        void Interject(Vector3 pos, Vector3 prevPos, Vector3 vel);
        void SetValue(float value);
    }
}