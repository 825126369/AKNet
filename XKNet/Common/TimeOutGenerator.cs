namespace XKNet.Common
{
#if DEBUG
    public class TimeOutGenerator
#else
    internal class TimeOutGenerator
#endif
    {
        double fTime = 0;
        double fInternalTime = 0;

        public TimeOutGenerator(double fInternalTime = 1.0)
        {
            SetInternalTime(fInternalTime);
        }

        public void SetInternalTime(double fInternalTime)
        {
            this.fInternalTime = fInternalTime;
            this.Reset();
        }

        public void Reset()
        {
            this.fTime = 0.0;
        }

        public bool orTimeOut(double fElapsed)
        {
            this.fTime += fElapsed;
            if (this.fTime > fInternalTime)
            {
                this.Reset();
                return true;
            }

            return false;
        }

        public bool orTimeOutWithSpecialTime(double fElapsed, float fInternalTime)
        {
            this.fTime += fElapsed;
            if (this.fTime > fInternalTime)
            {
                this.Reset();
                return true;
            }

            return false;
        }
    }
}
