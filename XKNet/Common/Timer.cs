using System;
using System.Collections.Generic;
using System.Text;

namespace XKNet.Common
{
    public class Timer
    {
        private DateTime nLastTime;

        public Timer()
        {
            restart();
        }

        public void restart()
        {
            nLastTime = DateTime.Now;
        }

        public double elapsed()
        {
            return (DateTime.Now - nLastTime).TotalSeconds;
        }
    }
}
