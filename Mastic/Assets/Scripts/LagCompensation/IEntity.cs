namespace Mastic
{
    public interface IEntity
    {
        void Initialize(LagCompensation lagCompensation);
        void RecordFrame(int tick);
        void SetAsTick(int tick);
        void ReturnToPresent();
    }
}