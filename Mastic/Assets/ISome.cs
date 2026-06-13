using Mirror;
using System.Collections.Generic;

namespace Mastic
{
    public interface ISome
    {
        void ReadFromServer(ref int index, bool alive, float[] data);
        void WriteToServer(List<uint> netIds, List<bool> alives, List<float> data);
    }

    public class TestEntity : NetworkBehaviour, ISome
    {
        private bool isAlive;

        public void ReadFromServer(ref int index, bool alive, float[] data)
        {
            isAlive = alive;

        }

        public void WriteToServer(List<uint> netIds, List<bool> alives)
        {
            throw new System.NotImplementedException();
        }
    }
}