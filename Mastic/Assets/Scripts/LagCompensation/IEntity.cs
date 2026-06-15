namespace Mastic
{
    public interface IEntity
    {
        byte DataSize { get; }
        void SavePresent();
        void DoRollback(int prevTick, int currTick, float lerpValue);
        void ReturnToPresent();
        void RecordFrame(int tick);
        void WriteToData(int index, float[] data);
        void ReadFromData(int index, float[] data, float time);
        void ReadFromDataReal(int index, float[] data, float time);
    }
}