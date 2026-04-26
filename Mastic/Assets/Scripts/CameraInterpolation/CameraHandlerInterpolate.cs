using UnityEngine;

namespace Mastic
{
    public class CameraHandlerInterpolate : MonoBehaviour, ICameraInterpolation
    {
        public float Value { get; set; }
        public bool IsInterjected { get; set; }

        private Vector3 pos;
        private Vector3 prevPos;
        private float time;

        private void Start()
        {
            pos = transform.position;
            prevPos = pos;
        }

        private void Update()
        {
            Value = Mathf.Clamp((Time.time - time) / Time.fixedDeltaTime, 0f, 1f);
            transform.position = Vector3.Lerp(prevPos, pos, Value);
        }

        public void Assign(Vector3 pos, Vector3 vel)
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
    }
}