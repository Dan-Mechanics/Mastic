using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// State of the player's movement at 
    /// int "tick" moment in time.
    /// </summary>
    public struct StateMessage
    {
        public int tick;
        public Vector3 position;
        public Vector3 velocity;
        public float yRotation;
        public float xRotation;

        public void SetValues(int tick, Vector3 position, Vector3 velocity, float yRotation, float xRotation)
        {
            this.tick = tick;
            this.position = position;
            this.velocity = velocity;
            this.yRotation = yRotation;
            this.xRotation = xRotation;
        }

        public void SetValues(Vector3 position, Vector3 velocity, InputMessage inputMessage)
        {
            tick = inputMessage.tick;
            this.position = position;
            this.velocity = velocity;
            yRotation = inputMessage.yRotation;
            xRotation = inputMessage.xRotation;
        }
    }
}