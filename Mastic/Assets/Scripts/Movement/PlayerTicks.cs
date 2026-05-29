namespace Mastic
{
    [System.Serializable]
    public class PlayerTicks 
    {
        // SERVER.
        public int serverTick;
        public int processedTick;
        
        // CLIENT.
        public int inputTick;
        public int rollbackTick;
        
        public override string ToString() 
            => $"inputTick: {inputTick}, serverTick: {serverTick}, receivedInputTick: {processedTick}, rollbackTick: {rollbackTick}.";
    }
}