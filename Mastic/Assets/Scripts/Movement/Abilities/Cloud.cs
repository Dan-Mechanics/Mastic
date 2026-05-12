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
        [SerializeField] private int maxPendingRequests = default;
        [SerializeField, Min(1)] private int expectedRigidbodies = default;
        [SerializeField] private int tickDuration = default;
        [SerializeField] private int standardTickrate = default;
        [SerializeField] private int cooldownIndex = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private GameObject cloudVisual;
        private float endTime;
        private float duration;
        private ForceZone forceZone;
        private int previousTick;
        private int startingTick;
        private int endingTick;

        private void Awake()
        {
            previousTick = -1;
            startingTick = -1;
            endingTick = -1;
            endTime = -1;
            cooldownHandler = GetComponent<CooldownHandler>();
            cloudVisual = Instantiate(cloudPrefab, cloudPrefab.transform.position, cloudPrefab.transform.rotation);
            cloudVisual.SetActive(false);
            forceZone = cloudVisual.GetComponent<ForceZone>();
            forceZone.Initialize(expectedRigidbodies, force);
            duration = (float)tickDuration / standardTickrate;
        }

        [Client]
        public void DoLocalTick(int movementTick, IMovement movement)
        {
            if (!ability2.IsHeld || !CanCast(movementTick))
                return;

            cooldownHandler.Cast(cooldownIndex);
            pendingRequests.Add(movementTick);
            CmdRequestSmite(movementTick);
        }

        private bool IsAbilityActive(int tick) => tick >= startingTick && tick <= endingTick;

        private void FixedUpdate()
        {
            if (isLocalPlayer || isServer)
                return;

            // UNLOCAL CLIENT.
            // THIS IS AN APPROXIMATION.
            bool active = NetworkTime.time <= endTime;
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        [Command]
        private void CmdRequestSmite(int tick)
        {
            if (pendingRequests.Count >= maxPendingRequests || tick <= previousTick)
                return;

            pendingRequests.Add(tick);
            previousTick = tick;
            Debug.LogWarning($"{gameObject.name}: requested burning wings {tick} ...");
        }

        [Client]
        public void CheckAgainstTickClient(int tick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick == pendingRequests[i])
                    Cast(tick);
            }

            bool active = IsAbilityActive(tick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int movementTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanCast(movementTick))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Cast(movementTick);
                RpcCastCloud(transform.position, (float)NetworkTime.time + duration);
                pendingRequests.RemoveAt(i);
                break;
            }

            bool active = IsAbilityActive(movementTick);
            cloudVisual.SetActive(active);
            if (active)
                forceZone.DoTick();
        }

        [ClientRpc]
        private void RpcCastCloud(Vector3 position, float endTime)
        {
            if (isLocalPlayer)
                return;

            cloudVisual.transform.position = position;
            this.endTime = endTime;
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