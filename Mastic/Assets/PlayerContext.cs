using UnityEngine;

namespace Mastic
{
    public class PlayerContext : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void Setup()
        {
            tickrate.OnTickrateChanged += DisplayTickrate;

            networkPhysicsMovement.OnTick += DisplayTick;
            networkPhysicsMovement.OnCheatsChanged += DisplayCheats;

            networkPhysicsMovement.OnReconsileStateChanged += IndicateReconsile;
        }
    }
}
