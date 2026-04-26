using Mirror;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mastic
{
    public class Tickrate : NetworkBehaviour
    {
        public event Action<int> OnTickrateChanged;

        [Header("References")]

        [SerializeField] private NetworkPhysicsMovement networkPhysicsMovement = null;
        [SerializeField] private UnityEvent onDing = null;

        [Header("Settings")]

        [SerializeField] private int standardTickrate;
        [SerializeField] private float minTimeDilationTime = 0.5f;
        [SerializeField] private int drainedTickrate = 65;
        [SerializeField] private int fullTickrate = 63;
        [SerializeField] private int idealPendingCount = 2;

        private MasticNetworkManager networkManager;

        private int currentTickrate;
        private bool hasTimeDilation = true;

        private void Awake()
        {
            networkManager = GameObject.FindWithTag("NetworkManager").GetComponent<MasticNetworkManager>();
            networkPhysicsMovement.OnBeforeServerTick += TryApplyEffect;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (idealPendingCount != 2) { Debug.LogWarning("maxIdealPendingInputCount != 2."); }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            SetTickrate(fullTickrate, true);
        }

        #region Tickrate

        [Server]
        private void TryApplyEffect(int pendingInputMessagesCount)
        {
            if (!networkPhysicsMovement.HasReceivedFirstMessage) { return; }
            if (hasTimeDilation) { return; }

            hasTimeDilation = true;

            TargetApplyTickrate(connectionToClient, (byte)CalculateTickrate(pendingInputMessagesCount));
        }

        [Server]
        private int CalculateTickrate(int pendingInputMessagesCount)
        {
            if (pendingInputMessagesCount < 1) { return drainedTickrate; }
            else if (pendingInputMessagesCount > idealPendingCount) { return fullTickrate; }

            return standardTickrate;
        }

        [TargetRpc]
        private void TargetApplyTickrate(NetworkConnectionToClient conn, byte tickrate)
        {
            if (currentTickrate == tickrate) { Invoke(nameof(CmdStopTimeDilation), minTimeDilationTime); return; }

            SetTickrate(tickrate);
        }

        [Client]
        private void SetTickrate(int tickrate, bool calledFromStart = false)
        {
            if (tickrate != standardTickrate && Application.isFocused && !calledFromStart) { onDing?.Invoke(); }

            currentTickrate = tickrate;

            Time.fixedDeltaTime = 1f / tickrate;
            networkManager.sendRate = tickrate;

            OnTickrateChanged?.Invoke(currentTickrate);

            Invoke(nameof(CmdStopTimeDilation), minTimeDilationTime);
        }

        [Command]
        private void CmdStopTimeDilation()
        {
            hasTimeDilation = false;
        }

        #endregion
    }
}