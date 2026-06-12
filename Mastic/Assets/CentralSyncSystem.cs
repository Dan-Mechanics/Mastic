using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mirror;

namespace Mastic
{
    public class CentralSyncSystem : NetworkBehaviour
    {
        private readonly Dictionary<uint, Some> dict = new Dictionary<uint, Some>();
        private readonly StringBuilder builder = new StringBuilder();
        private const char SPLITTER = ',';

        /*private string Format()
        {
            builder.Clear();
            foreach (var item in dict)
            {
                builder.Append(item.Value.pos.x).Append(SPLITTER);
                builder.Append(item.Value.pos.y).Append(SPLITTER);
                builder.Append(item.Value.pos.z).Append(SPLITTER);
                builder.Append(item.Value.xRot).Append(SPLITTER);
                builder.Append(item.Value.yRot).Append(SPLITTER);
            }

            return builder.ToString();
        }*/

        /*public Some Register(NetworkBehaviour behaviour)
        {
            
            return new Some();
        }*/

        [ClientRpc]
        private void RpcSync(float[] data)
        {

        }

        public interface Some
        {
            void SetAsData(float x, float y, float z, float xRot, float yRot, float special);
        }

        public struct Frame
        {
            public float x;
            public float y;
            public float z;
            public float xRot;
            public float yRot;
            public float special;
        }
    }
}
