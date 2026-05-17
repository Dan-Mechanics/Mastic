using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Cloud : NetworkBehaviour, IMovementAbility
    {
        public int magicOffset;
        [SerializeField] private EasyBinding ability2 = default;
        [SerializeField] private GameObject cloudPrefab = default;
        [SerializeField] private Vector3 force = default;
        [SerializeField] private int maxPendingRequests = default;
        [SerializeField, Min(1)] private int expectedRigidbodies = default;
        [SerializeField] private int cooldownIndex = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private GameObject cloudVisual;
        private ForceZone forceZone;
        private float standardInterval;
        private int tickDuration;
        private int previousTick;
        private int startingTick;
        private int endingTick;
        private int prev;

        private void Awake()
        {
            cooldownHandler = GetComponent<CooldownHandler>();
            cloudVisual = Instantiate(cloudPrefab, cloudPrefab.transform.position, cloudPrefab.transform.rotation);
            cloudVisual.SetActive(false);

            EasySettings easySettings = FindAnyObjectByType<EasySettings>();
            easySettings.Get(nameof(Cloud) + nameof(tickDuration), ref tickDuration);

            int standardTickrate = default;
            easySettings.Get(nameof(standardTickrate), ref standardTickrate);
            standardInterval = 1f / standardTickrate;

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
            Debug.LogWarning($"{gameObject.name}: requested burning wings {inputTick} ...");
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
            serverTick = Utils.GetCurrentServerTick(NetworkTime.time, standardInterval);
            if (serverTick - prev != 1)
                Debug.LogWarning($"if ({serverTick} - {prev} != 1)");

            prev = serverTick;
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanCast(serverTick))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Cast(transform.position, serverTick);
                RpcCast(transform.position, serverTick);
                pendingRequests.RemoveAt(i);
                break;
            }

            bool active = IsAbilityActive(serverTick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        [ClientRpc]
        private void RpcCast(Vector3 position, int syncedServerTick)
        {
            if (isLocalPlayer)
                return;

            Cast(position, syncedServerTick);
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