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
        public byte movementIndex;
        public float xRotation;
        public float yRotation;
        public int tick;

        public void SetValues(Vector3 position, Vector3 velocity, byte movementIndex, InputMessage inputMessage)
        {
            this.position = position;
            this.velocity = velocity;
            this.movementIndex = movementIndex;
            xRotation = inputMessage.xRotation;
            yRotation = inputMessage.yRotation;
            tick = inputMessage.tick;
        }
    }
}