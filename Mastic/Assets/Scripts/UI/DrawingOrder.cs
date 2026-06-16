using UnityEngine;

namespace Mastic
{
    public class DrawingOrder : MonoBehaviour
    {
        public void BringToFront()
        {
            Transform parent = transform.parent;
            transform.SetParent(null);
            transform.SetParent(parent);
        }
    }
}
