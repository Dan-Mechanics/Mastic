namespace Mastic
{
   /// <summary>
   /// TODO: FIX THIS, THIS IS BROKEN.
   /// </summary>
    public struct Timer 
    {
        public float interval;
        public float value;

        public Timer(float interval, float value = 0f)
        {
            this.interval = interval;
            this.value = value;
        }

        public bool Tick(float dt)
        {
            value += dt;
            if (value < interval)
                return false;

            value = 0f;
            return true;
        }
    }
}
