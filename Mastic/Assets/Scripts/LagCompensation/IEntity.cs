namespace Mastic
{
    public interface IEntity
    {
        /// <summary>
        ///  THis needs to be removed.
        /// </summary>
        /// <returns></returns>
        int GetNetId();
        void RecordFrame(int tick, int maxRecordingLength);
        void SavePresent();
        void SetAsTick(int tick);
        void ReturnToPresent();
    }
}