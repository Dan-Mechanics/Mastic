namespace Mastic
{
    public interface IMovementAbility
    {
        void DoLocalTick(int movementTick, IMovement movement);
        void CheckAgainstTick(int tick, IMovement movement);
        void CleanTicks(int upTo);
    }
}
