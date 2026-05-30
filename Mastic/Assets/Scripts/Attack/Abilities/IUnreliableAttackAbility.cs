namespace Mastic
{
    public interface IUnreliableAttackAbility : IDamageFeedback
    {
        void DoLocalUpdate(int inputTick);
        void DoServerTick(int processedTick, int serverTick, InputMessage inputMessage);
    }
}