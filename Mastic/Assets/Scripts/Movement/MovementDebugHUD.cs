using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class MovementDebugHUD : MonoBehaviour
    {
        [SerializeField] private Text tickrateText = default;
        [SerializeField] private Text tickText = default;
        [SerializeField] private Text cheatsText = default;
        [SerializeField] private GameObject reconsileIndicator = default;
        [SerializeField] private GameObject authPrefab = default;
        private int standardTickrate;
        private Transform authGraphic;

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

        public void DisplayServerState(StateMessage stateMessage)
        {
            if (authGraphic == null)
                authGraphic = Instantiate(authPrefab).transform;

            Vector3 pos = stateMessage.position;
            Quaternion rot = Quaternion.AngleAxis(stateMessage.yRotation, Vector3.up);
            authGraphic.SetPositionAndRotation(pos, rot);
        }
    }
}