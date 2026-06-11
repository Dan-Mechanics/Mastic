using UnityEngine;

namespace Mastic
{
    public interface IReliableAttackAbility : IDamageFeedback
    {
        void DoLocalUpdate(int inputTick, int rollbackTick);
        void DoServerTick(int processedTick);
    }
}