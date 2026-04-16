using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// https://docs.google.com/document/d/1ruX-Pqfwd8WIK8eIo0dRAs86pEsc5Z398IOfDLNskYc/edit?tab=t.0#bookmark=id.dgqfcmsnswq8
    /// </summary>
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