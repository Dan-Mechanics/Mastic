using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class MovementDebugHUD : MonoBehaviour
    {
        [SerializeField] private Text tickrateText = null;
        [SerializeField] private Text tickText = null;
        [SerializeField] private Text cheatsText = null;
        [SerializeField] private GameObject reconsileIndicator = null;
        private int standardTickrate;

        public void Setup(int standardTickrate) => this.standardTickrate = standardTickrate;

        public void DisplayTickrate(int tickrate) 
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

        public void DisplayTick(int tick) => tickText.text = tick.ToString();
        public void DisplayCheats(string cheats) => cheatsText.text = cheats;
        public void IndicateReconsile(bool value) => reconsileIndicator.SetActive(value);
    }
}