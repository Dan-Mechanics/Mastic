using UnityEngine;

namespace Mastic
{
    public interface IMovement
    {
        byte Index { get; set; }
        bool IsGrounded { get; }
        public void Move(float vert, float hori, float interval);
    }
}