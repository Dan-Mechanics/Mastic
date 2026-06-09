namespace Mastic
{
    public interface IDamagable
    {
        bool Damage(float amount);
        void Die();
    }
}