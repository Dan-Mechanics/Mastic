namespace Mastic
{
    [System.Serializable]
    public class SharedPlayerFields 
    {
        public int inputTick;
        public int serverTick;
        public int rollbackTick;
        
        public override string ToString() 
            => $"inputTick: {inputTick}, serverTick: {serverTick}, rollbackTick: {rollbackTick}";
    }
}