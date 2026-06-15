namespace Mastic
{
    public interface IEntity
    {
        byte DataSize { get; }
        void SavePresent();
        void DoRollback(int prevTick, int currTick, float lerpValue, bool isCleanSlate);
        void ReturnToPresent();
        void RecordFrame(int tick);
        void WriteToData(int index, float[] data);
        void MakeCleanSlate();
        void ReadFromDataReal(int index, float[] data, float time);
    }
}