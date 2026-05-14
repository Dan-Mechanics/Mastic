using System;

namespace Mastic
{
    public interface IDamageFeedback
    {
        event Action<float> OnAuthoritativeDamage;
        event Action<float> OnPredictDamage;
    }
}