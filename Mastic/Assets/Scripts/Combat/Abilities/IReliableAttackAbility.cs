using UnityEngine;

namespace Mastic
{
    public interface IReliableAttackAbility : IDamageFeedback
    {
        void DoLocalUpdate(int inputTick, int rollbackTick, float unlocalLerpValue);
        void DoServerTick(int processedTick);
    }
}