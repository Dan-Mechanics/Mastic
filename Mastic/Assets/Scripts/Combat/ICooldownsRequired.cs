namespace Mastic
{
    public interface ICooldownsRequired
    {
        void AssignCooldowns(Cooldown[] cooldowns, CooldownHandler cooldownHandler);
    }
}