using UnityEngine;

namespace Mastic
{
    public class CameraHandlerInterpolate : CameraHandler
    {
        private Vector3 prevPos;

        protected override void Start()
        {
            base.Start();

            prevPos = pos;
        }

        private void Update()
        {
            value = Mathf.Clamp((Time.time - time) / Time.fixedDeltaTime, 0f, 1f);
            transform.position = Vector3.Lerp(prevPos, pos, value);
        }

        public override void Assign(Vector3 pos, Vector3 vel)
        {
            base.Assign(pos, vel);

            prevPos = this.pos;
            this.pos = pos;
        }

        public override void Interject(Vector3 pos, Vector3 prevPos, Vector3 vel)
        {
            base.Interject(pos, prevPos, vel);

            this.pos = pos;
            this.prevPos = prevPos;
        }
    }
}