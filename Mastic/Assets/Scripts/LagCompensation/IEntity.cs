namespace Mastic
{
    /// <summary>
    /// https://docs.google.com/document/d/1ruX-Pqfwd8WIK8eIo0dRAs86pEsc5Z398IOfDLNskYc/edit?tab=t.0#bookmark=id.85idnj5zfq4s
    /// </summary>
    public interface IEntity
    {
        int GetNetId();
        void RecordFrame(int tick, int maxRecordingLength);
        void SavePresent();
        void RewindTime(int tick);
        void ReturnToPresent();
    }
}