/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class ThreadWorker
    {
        private AutoResetEvent mEventQReady = new AutoResetEvent(false);

        private void InitThreadWorker()
        {
            Thread mThread = new Thread(ThreadFunc);
            mThread.IsBackground = true;
            mThread.Start();
        }

        private void ThreadFunc()
        {
            while (true)
            {
                mEventQReady.WaitOne();
                //foreach (var v in mConnectionPeerDic)
                //{
                //    v.Value.Update();
                //}
            }
        }
    }
}









