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
        private PlayerInitializer playerComponentRemover;

        private void Awake()
        {
            // GET ALL THE STUFF.
            playerComponentRemover = GetComponent<PlayerInitializer>();
        }

        private void Start()
        {
            // SET ALL THE STUFF.
            playerComponentRemover.Setup(isServer, isLocalPlayer);
            if(isLocalPlayer)
            {
                SetupClient();
            }
        }

        private void DoClientSetup(int standardTickrate)
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
