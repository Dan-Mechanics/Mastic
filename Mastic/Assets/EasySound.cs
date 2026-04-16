using UnityEngine;

namespace Mastic
{
    [RequireComponent(typeof(AudioSource))]
    public class EasySound : MonoBehaviour
    {
        [SerializeField] private AudioClip clip = null;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();   
        }

        public void Play() 
        {
            if(source == null) { return; }

            source.PlayOneShot(clip);
        }
    }
}