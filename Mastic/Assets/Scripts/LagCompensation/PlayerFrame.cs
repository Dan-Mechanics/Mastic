using UnityEngine;

namespace Mastic
{
    public struct PlayerFrame
    {
        public Vector3 pos;
        public int tick;

        public PlayerFrame(Vector3 pos, int tick)
        {
            this.pos = pos;
            this.tick = tick;
        }
    }
}