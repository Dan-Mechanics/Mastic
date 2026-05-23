using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Cloud : NetworkBehaviour, IMovementAbility
    {
        [SerializeField] private EasyBinding ability2 = default;
        [SerializeField] private GameObject cloudPrefab = default;
        [SerializeField] private Vector3 force = default;
        [SerializeField, Min(1)] private int expectedRigidbodies = default;
        [SerializeField] private int cooldownIndex = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private ServerSequence serverSequence;
        private int maxPendingRequests;
        private GameObject cloudVisual;
        private ForceZone forceZone;

        private int tickDuration;
        private int previousTick;
        private int startingTick;
        private int endingTick;

        private void Awake()
        {
            cooldownHandler = GetComponent<CooldownHandler>();
            serverSequence = FindAnyObjectByType<ServerSequence>();
            cloudVisual = Instantiate(cloudPrefab, cloudPrefab.transform.position, cloudPrefab.transform.rotation);
            cloudVisual.SetActive(false);

            var easySettings = EasySettings.Current;
            tickDuration = easySettings.Get<int>(GetType().Name + nameof(tickDuration));
            maxPendingRequests = easySettings.Get<int>(nameof(maxPendingRequests));

            forceZone = cloudVisual.GetComponent<ForceZone>();
            forceZone.Initialize(expectedRigidbodies, force);
            previousTick = -1;
            startingTick = -1;
            endingTick = -1;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isLocalPlayer)
                EventManager<int>.AddListener(Occasion.DoUnlocalMovementAbilities, DoUnlocalTick);
        }

        private void OnDestroy()
        {
            EventManager<int>.RemoveListener(Occasion.DoUnlocalMovementAbilities, DoUnlocalTick);
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (!ability2.IsHeld || !CanCast(inputTick))
                return;

            cooldownHandler.Cast(cooldownIndex);
            pendingRequests.Add(inputTick);
            CmdRequestCast(inputTick);
        }

        private bool IsAbilityActive(int tick) => tick >= startingTick && tick <= endingTick;

        [Command]
        private void CmdRequestCast(int inputTick)
        {
            if (pendingRequests.Count >= maxPendingRequests || inputTick <= previousTick)
                return;

            pendingRequests.Add(inputTick);
            previousTick = inputTick;
            print($"{gameObject.name}: requested {GetType().Name} on {inputTick} ...");
        }

        [Client]
        public void CheckAgainstTickClient(int inputTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick == pendingRequests[i])
                    Cast(transform.position, inputTick);
            }

            bool active = IsAbilityActive(inputTick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        [Client]
        private void DoUnlocalTick(int syncedServerTick)
        {
            bool active = IsAbilityActive(syncedServerTick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanCast(serverTick))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Cast(transform.position, serverTick);
                pendingRequests.RemoveAt(i);

                // NOW SEND IT TO THE UNLOCAL CLIENTS.
                List<ServerSequence.Temp> temps = new List<ServerSequence.Temp>();
                serverSequence.GetPlayerTemp(temps);
                foreach (var temp in temps)
                {
                    // dont send to self. is this smart?
                    if (temp.connection == connectionToClient)
                    {
                        // THIS IS ALWAYS CORRECT.
                        TargetCast(temp.connection, transform.position, temp.processedTick + 1);
                        continue;
                    }

                    int orderOffset = Mathf.Clamp(temp.index, 0, 1);
                    TargetCast(temp.connection, transform.position, temp.processedTick + orderOffset);
                }

               // RpcCast(transform.position, serverTick);
                break;
            }

            bool active = IsAbilityActive(serverTick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        /*[ClientRpc]
        private void RpcCast(Vector3 position, int syncedServerTick)
        {
            if (isLocalPlayer)
                return;

            Cast(position, syncedServerTick);
        }*/

        [TargetRpc(channel = Channels.Unreliable)]
        private void TargetCast(NetworkConnectionToClient conn, Vector3 position, int inputTick)
        {
            Cast(position, inputTick);
        }

        public void CleanPendingRequests(int upTo)
        {
            for (int i = pendingRequests.Count - 1; i >= 0; i--)
            {
                if (pendingRequests[i] <= upTo)
                    pendingRequests.RemoveAt(i);
            }
        }

        private bool CanCast(int tick)
        {
            return !IsAbilityActive(tick) &&
                cooldownHandler.CanCast(cooldownIndex);
        }

        private void Cast(Vector3 position, int startTick)
        {
            cloudVisual.transform.position = position;
            startingTick = startTick;
            endingTick = startingTick + tickDuration;
        }
    }
}