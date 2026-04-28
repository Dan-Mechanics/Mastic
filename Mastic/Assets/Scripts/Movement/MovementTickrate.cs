using Mirror;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mastic
{
    public class MovementTickrate : NetworkBehaviour
    {
        public event Action<int> OnTickrateChanged;

        [SerializeField] private float minTimeDilationTime = 0.5f;
        [SerializeField] private int drainedTickrateOffset = 1;
        [SerializeField] private int fullTickrateOffset = -1;
        [SerializeField] private int idealPendingCount = 2;
        [SerializeField] private UnityEvent onTickrateChanged = default;
        private NetworkManager networkManager;
        private int standardTickrate;
        private int currentTickrate;
        private bool hasTimeDilation;

        public void Setup(NetworkManager networkManager, int standardTickrate)
        {
            this.standardTickrate = standardTickrate;
            this.networkManager = networkManager;
            hasTimeDilation = true;
        }
        
        [Client]
        public void LocalClientSetup() => SetTickrate(fullTickrateOffset, true);

        [Server]
        public void ApplyTimeDilation(bool hasReceivedFirstMessage, int pendingCount)
        {
            if (!hasReceivedFirstMessage || hasTimeDilation)
                return;

            hasTimeDilation = true;
            TargetApplyTickrate(connectionToClient, GetCurrentTickrate(pendingCount));
        }

        [Server]
        private int GetCurrentTickrate(int pendingCount)
        {
            if (pendingCount < 1)
            {
                return standardTickrate + drainedTickrateOffset;
            }
            else if (pendingCount > idealPendingCount)
            {
                return standardTickrate + fullTickrateOffset;
            }
            else
            {
                return standardTickrate;
            }
        }

        [TargetRpc]
        private void TargetApplyTickrate(NetworkConnectionToClient conn, int tickrate)
        {
            if (currentTickrate != tickrate)
            {
                SetTickrate(tickrate);
            }
            else
            {
                CancelInvoke(nameof(CmdStopTimeDilation));
                Invoke(nameof(CmdStopTimeDilation), minTimeDilationTime);
            }
        }

        [Client]
        private void SetTickrate(int tickrate, bool calledFromStart = false)
        {
            if (tickrate != standardTickrate && Application.isFocused && !calledFromStart)
                onTickrateChanged?.Invoke();

            currentTickrate = tickrate;
            Time.fixedDeltaTime = 1f / tickrate;
            networkManager.sendRate = tickrate;

            OnTickrateChanged?.Invoke(currentTickrate);

            CancelInvoke(nameof(CmdStopTimeDilation));
            Invoke(nameof(CmdStopTimeDilation), minTimeDilationTime);
        }

        [Command]
        private void CmdStopTimeDilation() => hasTimeDilation = false;
    }
}