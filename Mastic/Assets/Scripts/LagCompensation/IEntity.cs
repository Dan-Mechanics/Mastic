namespace Mastic
{
    public interface IEntity
    {
        int GetNetId();
        void RecordFrame(int tick, int maxRecordingLength);
        void SavePresent();
        void RewindTime(int tick);
        void ReturnToPresent();
    }
}