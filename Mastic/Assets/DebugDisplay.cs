using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class DebugDisplay : MonoBehaviour
    {
        [SerializeField] private Text tickrateText = null;
        [SerializeField] private Text currentTickText = null;
        [SerializeField] private Text cheatsText = null;
        [SerializeField] private GameObject reconsileIndicator = null;

        [SerializeField] private Tickrate tickrate = null;
        [SerializeField] private NetworkPhysicsMovement networkPhysicsMovement = null;

        private void Awake()
        {
            tickrate.OnTickrateChanged += RefreshTickrateDisplay;

            networkPhysicsMovement.OnCurrentTickChanged += RefreshCurrentTickDisplay;
            networkPhysicsMovement.OnCheatsChanged += RefreshCheatsDisplay;

            networkPhysicsMovement.OnReconsileStateChanged += RefreshReconsileIndicator;
        }

        private void RefreshTickrateDisplay(int newTickrate) 
        {
            tickrateText.text = newTickrate.ToString();

            // i dont know if these colors are perfect but its more about the vibe anyway ...
            if (newTickrate > MasticNetworkManager.STANDARD_TICKRATE) { tickrateText.color = Color.green; }
            else if (newTickrate < MasticNetworkManager.STANDARD_TICKRATE) { tickrateText.color = Color.red; }
            else { tickrateText.color = Color.white; }
        }

        private void RefreshCurrentTickDisplay(int currentTick)
        {
            currentTickText.text = currentTick.ToString();
        }

        private void RefreshCheatsDisplay(string cheats)
        {
            cheatsText.text = cheats;
        }

        private void RefreshReconsileIndicator(bool show) 
        {
            reconsileIndicator.SetActive(show);
        }
    }
}