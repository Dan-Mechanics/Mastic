using UnityEngine;

namespace Mastic
{
    public class CameraHandlerExtrapolate : CameraHandler
    {
        private Vector3 vel;

        private void Update()
        {
            value = Time.time - time;
            transform.position = pos + (vel * value);
        }

        public override void Assign(Vector3 pos, Vector3 vel)
        {
            base.Assign(pos, vel);
            this.pos = pos;
            this.vel = vel;
        }

        public override void Interject(Vector3 pos, Vector3 prevPos, Vector3 vel)
        {
            base.Interject(pos, prevPos, vel);
            this.pos = pos;
            this.vel = vel;
        }
    }
}