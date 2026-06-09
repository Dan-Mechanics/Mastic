using UnityEngine;

namespace Mastic
{
    public interface ICameraInterpolation
    {
        float LerpValue { get; set; }
        float MaxLerpValue { get; set; }
        bool IsInterjected { get; set; }
        void Apply(Vector3 pos, Vector3 vel);
        void Interject(Vector3 pos, Vector3 prevPos, Vector3 vel);
        void SetValue(float value);
    }
}