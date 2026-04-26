using Mirror;
using UnityEngine;

namespace Mastic
{
    public class PlayerContext : MonoBehaviour
    {
        [SerializeField] private Tickrate tickrate = default;
        [SerializeField] private DebugDisplay debugDisplay = default;

        [Client]
        public void Setup(int standardTickrate)
        {
            Tickrate tickrate = GetComponent<Tickrate>();
            DebugDisplay debugDisplay = GetComponent<DebugDisplay>();
            NetworkPhysicsMovement networkPhysicsMovement = GetComponent<NetworkPhysicsMovement>();

            debugDisplay.Setup(standardTickrate);

            tickrate.OnTickrateChanged += debugDisplay.DisplayTickrate;

            networkPhysicsMovement.OnTick += debugDisplay.DisplayTick;
            networkPhysicsMovement.OnCheatsChanged += debugDisplay.DisplayCheats;

            networkPhysicsMovement.OnReconsileStateChanged += debugDisplay.IndicateReconsile;
        }
    }
}
