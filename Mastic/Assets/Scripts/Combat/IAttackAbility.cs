using System;

namespace Mastic
{
    public interface IAttackAbility : IDamageFeedback
    {
        void DoLocalUpdate(int movementTick, int rollbackTick);
        void DoServerTick(int movementTick);
    }
}