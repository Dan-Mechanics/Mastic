namespace Mastic
{
    public interface IEntity
    {
        void RecordFrame(int tick, int maxRecordingLength);
        void SetAsTick(int tick);
        void ReturnToPresent();
    }
}