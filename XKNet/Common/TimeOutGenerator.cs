/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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
