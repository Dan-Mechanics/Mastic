using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class DebugDisplay : MonoBehaviour
    {
        [SerializeField] private Text tickrateText = null;
        [SerializeField] private Text tickText = null;
        [SerializeField] private Text cheatsText = null;
        [SerializeField] private GameObject reconsileIndicator = null;

        [SerializeField] private Tickrate tickrate = null;
        [SerializeField] private NetworkPhysicsMovement networkPhysicsMovement = null;

        private int standardTickrate;

        public void Setup(int standardTickrate) => this.standardTickrate = standardTickrate;

        private void Awake()
        {
            tickrate.OnTickrateChanged += DisplayTickrate;

            networkPhysicsMovement.OnTick += DisplayTick;
            networkPhysicsMovement.OnCheatsChanged += DisplayCheats;

            networkPhysicsMovement.OnReconsileStateChanged += IndicateReconsile;
        }

        private void DisplayTickrate(int tickrate) 
        {
            tickrateText.text = tickrate.ToString();
            if (tickrate > standardTickrate)
            {
                tickrateText.color = Color.green;
            }
            else if (tickrate < standardTickrate) 
            { 
                tickrateText.color = Color.red;
            }
            else
            {
                tickrateText.color = Color.white;
            }
        }

        private void DisplayTick(int tick) => tickText.text = tick.ToString();
        private void DisplayCheats(string cheats) => cheatsText.text = cheats;
        private void IndicateReconsile(bool value) => reconsileIndicator.SetActive(value);
    }
}