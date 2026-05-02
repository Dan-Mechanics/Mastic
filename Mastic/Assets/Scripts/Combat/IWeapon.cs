namespace Mastic
{
    /// <summary>
    /// Consider renaming to IAttack instead.
    /// </summary>
    public interface IWeapon
    {
        void DoLocalTick(int movementTick, int rollbackTick);
        void DoServerTick(int movementTick);
    }
}