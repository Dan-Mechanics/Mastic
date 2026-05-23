namespace Mastic
{
    [System.Serializable]
    public class SharedPlayerFields 
    {
        // SERVER.
        public int serverTick;
        public int receivedInputTick;
        
        // CLIENT.
        public int inputTick;
        public int rollbackTick;
        
        public override string ToString() 
            => $"inputTick: {inputTick}, serverTick: {serverTick}, receivedInputTick: {receivedInputTick}, rollbackTick: {rollbackTick}.";
    }
}