using UnityEngine;

namespace Mastic
{
    public interface IMovement
    {
        byte Index { get; set; }
        bool IsGrounded { get; }
        void Move(float vert, float hori, float interval);
        void AddForce(Vector3 velocityChange);
    }
}