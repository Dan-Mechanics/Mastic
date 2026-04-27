namespace Mastic
{
    public interface IEntity
    {
        void RecordFrame(int tick, int maxRecordingLength);
        void SavePresent();
        void SetAsTick(int tick);
        void ReturnToPresent();
    }
}