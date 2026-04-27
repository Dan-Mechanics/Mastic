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
            transform.position = pos + (vel * Value);
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
    }
}