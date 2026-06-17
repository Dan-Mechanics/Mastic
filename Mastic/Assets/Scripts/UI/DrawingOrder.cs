using UnityEngine;

namespace Mastic
{
    public class DrawingOrder : MonoBehaviour
    {
        public void BringToFront()
            => transform.SetAsLastSibling();

        public void SendToBack()
            => transform.SetAsFirstSibling();
    }
}
