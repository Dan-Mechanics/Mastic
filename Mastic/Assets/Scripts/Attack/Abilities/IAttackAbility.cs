using System;

namespace Mastic
{
    public interface IAttackAbility : IDamageFeedback
    {
        void DoLocalUpdate(int inputTick, int rollbackTick);
        void DoServerTick(int inputTick);
    }
}