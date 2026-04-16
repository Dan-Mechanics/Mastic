using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Todo: check if this already exists.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EasySound : MonoBehaviour
    {
        [SerializeField] private AudioClip clip = default;
        private AudioSource source;

        private void Awake() => source = GetComponent<AudioSource>();
        public void Play() => source.PlayOneShot(clip);
    }
}