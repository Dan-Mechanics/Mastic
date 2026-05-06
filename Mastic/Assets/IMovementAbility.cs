namespace Mastic
{
    public interface IMovementAbility
    {
        void DoLocalUpdate(int movementTick);
        void CheckAgainstTick(int tick, IMovement movement);
        void CleanTicks(int upTo);
    }
}
