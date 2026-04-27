using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// State of the player's movement at 
    /// int "tick" moment in time.
    /// </summary>
    public struct StateMessage
    {
        public Vector3 position;
        public Vector3 velocity;
        public float xRotation;
        public float yRotation;
        public int tick;

        public void SetValues(Vector3 position, Vector3 velocity, InputMessage inputMessage)
        {
            this.position = position;
            this.velocity = velocity;
            xRotation = inputMessage.xRotation;
            yRotation = inputMessage.yRotation;
            tick = inputMessage.tick;
        }
    }
}