namespace Mastic
{
    public interface IEntity
    {
        void Setup(LagCompensation lagCompensation);
        void RecordFrame(int tick);
        void SetAsTick(int tick);
        void ReturnToPresent();
    }
}