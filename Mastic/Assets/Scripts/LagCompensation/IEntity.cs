namespace Mastic
{
    public interface IEntity
    {
        int GetNetId();
        void RecordFrame(int tick, int maxRecordingLength);
        void SavePresent();
        void SetAsTick(int tick);
        void ReturnToPresent();
    }
}