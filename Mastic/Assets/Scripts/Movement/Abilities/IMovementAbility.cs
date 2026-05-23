namespace Mastic
{
    public interface IMovementAbility
    {
        void DoLocalTick(int inputTick, IMovement movement);
        void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick);
        void CheckAgainstTickClient(int inputTick, IMovement movement);
        void CleanPendingRequests(int upTo);
    }
}
