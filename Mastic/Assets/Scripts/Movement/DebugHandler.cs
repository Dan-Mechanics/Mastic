using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class DebugHandler : MonoBehaviour
    {
        [SerializeField] private Text tickrateText = default;
        [SerializeField] private Text tickText = default;
        [SerializeField] private Text cheatsText = default;
        [SerializeField] private GameObject reconsileIndicator = default;
        [SerializeField] private AudioClip reconsileSound = default;
        [SerializeField] private AudioClip tickrateChangeSound = default;
        [SerializeField] private GameObject authPrefab = default;
        [SerializeField] private Vector3 authOffset = default;
        private AudioSource source;
        private Transform authGraphic;
        private int standardTickrate;

        public void Initialize(int standardTickrate)
        {
            this.standardTickrate = standardTickrate;
            source = GetComponentInChildren<AudioSource>();
        }

        public void DisplayTick(int tick) 
            => tickText.text = tick.ToString();

        public void DisplayCheats(string cheats) 
            => cheatsText.text = cheats;

        public void DisplayReconsile(bool value) 
            => reconsileIndicator.SetActive(value);

        public void PlayReconsileSound() 
            => source.PlayOneShot(reconsileSound);

        public void PlayTickrateChangedSound() 
            => source.PlayOneShot(tickrateChangeSound);

        public void DisplayTickrate(int tickrate) 
        {
            tickrateText.color = Color.white;
            tickrateText.text = tickrate.ToString();
            if (tickrate > standardTickrate)
            {
                tickrateText.color = Color.green;
            }
            else if (tickrate < standardTickrate) 
            { 
                tickrateText.color = Color.red;
            }
        }

        public void DisplayServerState(StateMessage stateMessage)
        {
            if (authPrefab == null)
                return;

            if (authGraphic == null)
                authGraphic = Instantiate(authPrefab).transform;

            Vector3 pos = stateMessage.position + authOffset;
            Quaternion rot = Quaternion.AngleAxis(stateMessage.yRotation, Vector3.up);
            authGraphic.SetPositionAndRotation(pos, rot);
        }
    }
}