using UnityEngine;

namespace Mastic
{
    public interface IMovement
    {
        bool IsGrounded { get; }
        public void Move(float vert, float hori, float interval);
        public void LimitSpeed();
        public void AddForce(Vector3 velocityChange);
    }
}