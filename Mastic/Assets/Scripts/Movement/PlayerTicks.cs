namespace Mastic
{
    [System.Serializable]
    public class PlayerTicks 
    {
        //public int rollbackTick;
        public InputMessage previousInputMessage;
        
       //public override string ToString() 
       //    => $"inputTick: {inputTick}, serverTick: {serverTick}, rollbackTick: {rollbackTick}.";
    }
}