using UnityEngine;

namespace Mastic
{
    public class CameraHandlerInterpolate : MonoBehaviour, ICameraInterpolation
    {
        public float LerpValue { get; set; }
        public float MaxLerpValue { get; set; }
        public bool IsInterjected { get; set; }
        private Vector3 pos;
        private Vector3 prevPos;
        private float time;

        private void Awake()
        {
            pos = transform.position;
            prevPos = pos;
        }

        private void Update()
        {
            LerpValue = (Time.time - time) / Time.fixedDeltaTime;
            SetValue(LerpValue);
        }

        public void Apply(Vector3 pos, Vector3 vel)
        {
            time = Time.time;
            IsInterjected = false;
            prevPos = this.pos;
            this.pos = pos;
        }

        public void Interject(Vector3 pos, Vector3 prevPos, Vector3 vel)
        {
            IsInterjected = true;
            this.pos = pos;
            this.prevPos = prevPos;
        }

        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/Vector3.LerpUnclamped.html
        /// </summary>
        public void SetValue(float value)
        {
            value = Mathf.Clamp(value, 0f, MaxLerpValue);
            transform.position = Vector3.LerpUnclamped(prevPos, pos, value);
        }
    }
}