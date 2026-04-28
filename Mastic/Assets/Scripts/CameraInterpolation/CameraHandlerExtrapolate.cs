using UnityEngine;

namespace Mastic
{
    public class CameraHandlerExtrapolate : MonoBehaviour, ICameraInterpolation
    {
        public float Value { get; set; }
        public bool IsInterjected { get; set; }

        private Vector3 pos;
        private Vector3 vel;
        private float time;

        private void Start() => pos = transform.position;

        private void Update()
        {
            Value = Time.time - time;
            SetAsValue(Value);
        }

        public void Assign(Vector3 pos, Vector3 vel)
        {
            time = Time.time;
            IsInterjected = false;
            this.pos = pos;
            this.vel = vel;
        }

        public void Interject(Vector3 pos, Vector3 prevPos, Vector3 vel)
        {
            IsInterjected = true;
            this.pos = pos;
            this.vel = vel;
        }

        public void SetAsValue(float value)
        {
            value = Mathf.Clamp(value, 0f, Time.fixedDeltaTime);
            transform.position = pos + (vel * value);
        }
    }
}