using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class SyncSystem : NetworkBehaviour
    {
        List<uint> netIds = new List<uint>();
        List<bool> alives = new List<bool>();
        List<float> data = new List<float>();

        private Dictionary<uint, ISome> dict;

        private bool isAlive;

        public override void OnStartServer()
        {
            base.OnStartServer();
            InvokeRepeating(nameof(Test), 0.5f, 0.5f);
        }

        private MouseLook mouseLook;

        private void Test()
        {
            int tick = 0;
            netIds.Clear();
            alives.Clear();
            Save(netIds, alives);
            
            RpcSync(tick, netIds.ToArray(), alives.ToArray(), data.ToArray());
        }

        [ClientRpc]
        private void RpcSync(int tick, uint[] netIds, bool[] alives, float[] data)
        {
            if (!dict.ContainsKey(netId))
                return;

            if (dict[netId] == null)
            {
                dict.Remove(netId);
                return;
            }

            int index = 0;
            for (int i = 0; i < netIds.Length; i++)
            {
                dict[netIds[i]].ReadFromServer(ref index, alives[i], data);
            }
        }

        public void ReadFromServer(ref int index, bool alive, float[] data)
        {
            transform.position = new Vector3(
                data[index],
                data[index + 1],
                data[index + 2]);

         //   transform.position = frame.position;
            mouseLook.SetRotationTemporarily(data[index + 3], data[index + 4]);

            index += 5;

            // THIS IS WHERE LOOKBONE SHOULD GO.
            // INCLUDING HITBOX IF THAT IS NOT ATTACHED TO LOOKBONE.
           // if (!isLocalPlayer)
            //    lookBone.localRotation = Quaternion.Euler(0f, 0f, -frame.xRotation);
        }

        public void Save(List<uint> netIds, List<bool> alives, List<float> data)
        {
            netIds.Add(netId);
            alives.Add(isAlive);

            Vector3 pos = transform.position;
            data.Add(pos.x);
            data.Add(pos.y);
            data.Add(pos.z);
            data.Add(mouseLook.RotationX);
            data.Add(mouseLook.RotationY);
        }
    }
}
