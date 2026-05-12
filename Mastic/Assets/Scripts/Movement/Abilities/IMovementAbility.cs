namespace Mastic
{
    public interface IMovementAbility
    {
        void DoLocalTick(int movementTick, IMovement movement);
        void CheckAgainstTickServer(int inputTick, IMovement movement, int movementTick);
        void CheckAgainstTickClient(int tick);
        void CleanPendingRequests(int upTo);
    }
}
