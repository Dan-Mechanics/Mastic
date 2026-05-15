namespace Mastic
{
    [System.Serializable]
    public class SharedPlayerFields 
    {
        public int serverTick;
        public int receivedInputTick;

        public int inputTick;
        public int rollbackTick;
        
        public override string ToString() 
            => $"inputTick: {inputTick}, serverTick: {serverTick}, serverProcessedTick: {receivedInputTick}, rollbackTick: {rollbackTick}.";
    }
}