using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

namespace Mastic
{
    public class ServerSyncedTicks : MonoBehaviour
    {
        [Tooltip("In theory, this should not change.")]
        [SerializeField] private double interval = 0f;
        // on the other hand, the clients tickrate do be speeding up tho
        // so idk how that fucks with it.
        // if think that makes sense.
        // we can at least set the tick to the current one at the start 
        // but i guess there does have to be a difference in the cluetn tick and server
        // tick when it deviates from the thing with the effect or something idk.


        private void Start()
        {
            InvokeRepeating(nameof(Log), 0f, (float)interval);
        }

        private void Log() 
        {
            print(GetCurrentServerTick());
        }

        private uint GetCurrentServerTick() 
        {
            // time into ticks is time * tickrate
            // so that means time / interval = ticks
            // but then we can get 1.5 or 1.25 ticks and thats not possible of course
            // so then we round dowanrd so that 0.1 is 0 and 0.9 is 0
            // and that 1.1 is 1 etc.
            // its also impossible for a tick to be -10 so we make it uint.
            // but on the other hand that might be redundant since all the arrays take int.
            // so in the long run it doesnt really matter but alles wat niet mag kan niet so yeah.

            return (uint)Mathf.FloorToInt((float)(NetworkTime.time / interval));
        }
    }
}