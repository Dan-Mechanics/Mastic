using UnityEngine;

namespace Mastic
{
    [CreateAssetMenu(fileName = nameof(Tickrate), menuName = nameof(Tickrate))]
    public class Tickrate : ScriptableObject
    {
        [Min(1)] public int standardTickrate;
        [Min(1)] public int drainedTickrate;
        [Min(1)] public int fullTickrate;
        public float standardInterval;
        public float drainedInterval;
        public float fullInterval;

        private void OnValidate()
        {
            standardInterval = 1f / standardTickrate;
            drainedInterval = 1f / drainedTickrate;
            fullInterval = 1f / fullTickrate;
        }
    }
}
