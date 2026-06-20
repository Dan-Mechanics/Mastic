using Mirror;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace Mastic
{
    public class SoothingRose : NetworkBehaviour, IMovementAbility
    {
        [SerializeField] private EasyBinding binding = default;
        [SerializeField] private GameObject prefab = default;
        [SerializeField] private float range = default;
        [SerializeField] private LayerMask mask = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private int maxPendingRequests;
        private Transform cam;
        private PlayerEntity entity;
        private int cooldownIndex;
        private int previousTick;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            cooldownHandler = GetComponent<CooldownHandler>();
            cam = GameObject.FindWithTag("MainCamera").transform;
            cooldownIndex = cooldownHandler.GetIndexFromName(GetType().Name);

            EasySettings easySettings = EasySettings.Current;
            maxPendingRequests = easySettings.Get<int>(nameof(maxPendingRequests));
            previousTick = -1;
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            // IDEA: ADD CHAMBERING SO YOU CAN'T SPAM THIS ACCIDENTALLY.
            if (!binding.IsHeld || !cooldownHandler.CanCast(cooldownIndex))
                return;

            cooldownHandler.Cast(cooldownIndex);
            pendingRequests.Add(inputTick);
            CmdRequestCast(inputTick);
        }

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
        public void CheckAgainstTickClient(int inputTick, IMovement movement) { }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !cooldownHandler.CanCast(cooldownIndex))
                    continue;

                Vector3 point = Vector3.zero;
                if (!NetcodeUtils.GetAimingPoint(entity, cam, range, mask, ref point))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Cast(point);
                pendingRequests.RemoveAt(i);
                RpcCast(point);
                break;
            }
        }

        [ClientRpc]
        private void RpcCast(Vector3 pos)
            => Cast(pos);

        public void CleanPendingRequests(int upTo)
        {
            for (int i = pendingRequests.Count - 1; i >= 0; i--)
            {
                if (pendingRequests[i] <= upTo)
                    pendingRequests.RemoveAt(i);
            }
        }

        private void Cast(Vector3 pos)
        {
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            go.GetComponent<HealthPack>().Initialize(isServer);
        }
    }
}