using UnityEngine;

namespace Mastic
{
    public class CameraHandlerExtrapolate : CameraHandler
    {
        private Vector3 vel;

        private void Update()
        {
            //value = Mathf.Clamp(Time.time - time, 0f, 1f);
            value = Time.time - time;
            transform.position = pos + (vel * value);
        }

        public override void Assign(Vector3 pos, Vector3 vel)
        {
            base.Assign(pos, vel);

            // dont use rb.position, thats dumb.
            this.pos = pos;
            this.vel = vel;
        }

        public override void Interject(Vector3 pos, Vector3 prevPos, Vector3 vel)
        {
            base.Interject(pos, prevPos, vel);

            // think this might work.
            this.pos = pos;
            this.vel = vel;
        }
    }
}