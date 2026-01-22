/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;
using System.Threading;

namespace MSQuic2
{
    internal static partial class MSQuicFunc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatEventInitialize(out EventWaitHandle Event, bool ManualReset, bool InitialState)
        {
            if (ManualReset)
            {
                Event = new ManualResetEvent(InitialState);
            }
            else
            {
                Event = new AutoResetEvent(InitialState);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatEventUninitialize(EventWaitHandle Event)
        {
            Event.Close();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CxPlatEventSet(EventWaitHandle Event)
        {
             return Event.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CxPlatEventReset(EventWaitHandle Event)
        {
            return Event.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CxPlatEventWaitForever(EventWaitHandle Event)
        {
            return Event.WaitOne();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CxPlatEventWaitWithTimeout(EventWaitHandle Event, int TimeoutMs)
        {
            return Event.WaitOne(TimeoutMs);
        }

    }
}
