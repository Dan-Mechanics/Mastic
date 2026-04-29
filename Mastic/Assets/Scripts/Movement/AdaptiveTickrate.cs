using Mirror;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mastic
{
    public class AdaptiveTickrate : NetworkBehaviour
    {
        public event Action<int> OnTickrateChanged;

        [SerializeField] private float minTimeDilationTime = default;
        [SerializeField] private int drainedTickrateOffset = default;
        [SerializeField] private int fullTickrateOffset = default;
        [SerializeField] private int idealPendingCount = default;
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

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            SetTickrate(standardTickrate + fullTickrateOffset, true);
        }

        [Server]
        public void ApplyTimeDilation(bool hasReceivedFirstMessage, int pendingCount)
        {
            if (!hasReceivedFirstMessage || hasTimeDilation)
                return;

            hasTimeDilation = true;
            TargetApplyTickrate(connectionToClient, GetTickrate(pendingCount));
        }

        [Server]
        private int GetTickrate(int pendingCount)
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