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
        private int tickDuration;
        private int unlocalCurrentTick;
        private int standardTickrate;
        private int previousTick;
        private int startingTick;
        private int endingTick;

        private void Awake()
        {
            cooldownHandler = GetComponent<CooldownHandler>();
            cloudVisual = Instantiate(cloudPrefab, cloudPrefab.transform.position, cloudPrefab.transform.rotation);
            cloudVisual.SetActive(false);

            EasySettings easySettings = FindAnyObjectByType<EasySettings>();
            easySettings.Get(nameof(Cloud) + nameof(tickDuration), ref tickDuration);

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
            CmdRequestCloud(inputTick);
        }

        private bool IsAbilityActive(int tick) => tick >= startingTick && tick <= endingTick;

        [Command]
        private void CmdRequestCloud(int inputTick)
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
                    Cast(inputTick);
            }

            bool active = IsAbilityActive(inputTick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        [Client]
        private void DoUnlocalTick(int inputTick)
        {
            unlocalCurrentTick = inputTick;
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

                cooldownHandler.Cast(cooldownIndex);
                Cast(serverTick);
                RpcCastCloud(transform.position, NetworkTime.time);
                pendingRequests.RemoveAt(i);
                break;
            }

            bool active = IsAbilityActive(serverTick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        [ClientRpc]
        private void RpcCastCloud(Vector3 position, double sendTime)
        {
            if (isLocalPlayer)
                return;

            double diff = NetworkTime.time - sendTime;
            int tickDiff = Mathf.FloorToInt((float)(diff / Time.fixedDeltaTime));

            Cast(unlocalCurrentTick - tickDiff + magicOffset);
            cloudVisual.transform.position = position;
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

        private void Cast(int tick)
        {
            cloudVisual.transform.position = transform.position;
            startingTick = tick;
            endingTick = startingTick + tickDuration;
        }
    }
}