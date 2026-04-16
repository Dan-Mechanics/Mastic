using UnityEngine;

namespace Mastic
{
    public class CameraHandler : MonoBehaviour
    {
        public float Value => value;
        public bool IsInterjected => isInterjected;

        protected Vector3 pos;
        protected float time;

        protected float value;
        protected bool isInterjected;

        protected virtual void Start()
        {
            pos = transform.position;
        }

        public virtual void Assign(Vector3 pos, Vector3 vel) 
        {
            time = Time.time;
            isInterjected = false;
        }

        public virtual void Interject(Vector3 pos, Vector3 prevPos, Vector3 vel) 
        {
            isInterjected = true;
        }
    }
}