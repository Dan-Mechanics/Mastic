using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

namespace Mastic
{
    public class Smite : NetworkBehaviour
    {
        public bool IsStunned => isStunned;
        public bool IsSmithing => isSmithing;

        [SerializeField] private NetworkPhysicsMovement networkPhysicsMovement = null;

        private bool isStunned;
        private bool isSmithing;

        private int startSmitingTick;

        public void TrySmite() 
        {

        }

        public void Stun() 
        {
            isSmithing = false;
        }
    }
}