using System;

namespace Mastic
{
    public interface IAttack
    {
        event Action<float> OnDealDamage;
        event Action<float> OnPredictDamage;

        void DoLocalTick(int movementTick, int rollbackTick);
        void DoServerTick(int movementTick);
    }
}