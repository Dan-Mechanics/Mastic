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
        [SerializeField, Min(1)] private int expectedColliders = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private ServerSequence serverSequence;
        private int maxPendingRequests;
        private GameObject cloudVisual;
        private ForceZone forceZone;
        private string cooldownName;

        private int standardTickrate;
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
            cooldownName = nameof(Cloud).ToLowerInvariant();

            EasySettings easySettings = EasySettings.Current;
            tickDuration = easySettings.Get<int>(GetType().Name + nameof(tickDuration));
            maxPendingRequests = easySettings.Get<int>(nameof(maxPendingRequests));
            standardTickrate = easySettings.Get<int>(nameof(standardTickrate));

            forceZone = cloudVisual.GetComponent<ForceZone>();
            forceZone.Initialize(expectedColliders, 1f / standardTickrate, force);
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
            Destroy(cloudVisual);
            EventManager<int>.RemoveListener(Occasion.DoUnlocalMovementAbilities, DoUnlocalTick);
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            // IDEA: ADD CHAMBERING SO YOU CAN'T SPAM THIS ACCIDENTALLY.
            if (!ability2.IsHeld || !CanCast(inputTick))
                return;

            cooldownHandler.Cast(cooldownName);
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
        public void CheckAgainstTickClient(int inputTick, IMovement movement)
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
        private void DoUnlocalTick(int inputTick)
        {
            bool active = IsAbilityActive(inputTick);
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

                cooldownHandler.Cast(cooldownName);
                Cast(transform.position, serverTick);
                pendingRequests.RemoveAt(i);

                // SEND BACK TO PLAYERS.
                serverSequence.RemoveNullPlayers();
                int playerCount = serverSequence.PlayerCount;
                for (int j = 0; j < playerCount; j++)
                {
                    (NetworkConnectionToClient conn, int processedTick) = serverSequence.GetProcessedTick(j);
                    TargetCast(conn, transform.position, processedTick);
                }

                break;
            }

            bool active = IsAbilityActive(serverTick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        /// <summary>
        /// Make this reliable because otherwise
        /// you get even more reconsiled.
        /// </summary>
        [TargetRpc]
        private void TargetCast(NetworkConnectionToClient conn, Vector3 pos, int inputTick) => Cast(pos, inputTick);

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
                cooldownHandler.CanCast(cooldownName);
        }

        private void Cast(Vector3 pos, int startTick)
        {
            cloudVisual.transform.position = pos;
            startingTick = startTick;
            endingTick = startingTick + tickDuration;
        }
    }
}