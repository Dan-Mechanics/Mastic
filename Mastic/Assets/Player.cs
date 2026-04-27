using Mirror;
using Unity.Properties;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Context class managing the flow of the player sequence.
    /// This should most likely be a NetworkBehaviour
    /// </summary>
    public class Player : NetworkBehaviour
    {
        private PlayerSetup playerSetup;

        private void Awake()
        {
            // GET ALL THE STUFF.
            playerSetup = GetComponent<PlayerSetup>();
        }

        private void Start()
        {
            // SET ALL THE STUFF.
            playerSetup.Setup(isServer, isLocalPlayer);
            if(isLocalPlayer)
            {
                SetupClient();
            }
        }

        private void SetupClient(int standardTickrate)
        {
            Tickrate tickrate = GetComponent<Tickrate>();
            MovementDebugHUD debugDisplay = GetComponent<MovementDebugHUD>();
            NetworkPhysicsMovement networkPhysicsMovement = GetComponent<NetworkPhysicsMovement>();

            debugDisplay.Setup(standardTickrate);

            tickrate.OnTickrateChanged += debugDisplay.DisplayTickrate;

            networkPhysicsMovement.OnTick += debugDisplay.DisplayTick;
            networkPhysicsMovement.OnCheatsChanged += debugDisplay.DisplayCheats;

            networkPhysicsMovement.OnReconsileStateChanged += debugDisplay.IndicateReconsile;
        }
    }
}
