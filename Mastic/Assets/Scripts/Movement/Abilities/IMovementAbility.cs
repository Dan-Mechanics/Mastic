namespace Mastic
{
    public interface IMovementAbility
    {
        void DoLocalTick(int movementTick, IMovement movement);
        void CheckAgainstTickServer(int tick, IMovement movement, int movementTick);
        void CheckAgainstTickClient(int tick);
        void CleanTicks(int upTo);
    }
}
