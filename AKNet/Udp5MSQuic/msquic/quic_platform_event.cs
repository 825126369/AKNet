using AKNet.Common;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal class CXPLAT_EVENT
    {
        //ManualResetEventSlim 
        public readonly object Mutex = new object();
        public readonly object Cond = new object();
        public bool Signaled;
        public bool AutoReset;
    }

    public class CXPLAT_EVENT_2
    {
        private readonly object _lockObject = new object();
        private bool _signaled = false;
        private bool _autoReset = false;

        public CXPLAT_EVENT_2(bool autoReset)
        {
            _autoReset = autoReset;
        }

        public void Signal()
        {
            lock (_lockObject)
            {
                _signaled = true;
                Monitor.PulseAll(_lockObject); // 通知所有等待的线程
            }
        }

        public void Wait()
        {
            lock (_lockObject)
            {
                while (!_signaled)
                {
                    Monitor.Wait(_lockObject); // 等待信号
                }

                if (_autoReset)
                {
                    _signaled = false; // 如果是自动重置，重置信号状态
                }
            }
        }

        public bool IsSignaled
        {
            get
            {
                lock (_lockObject)
                {
                    return _signaled;
                }
            }
        }
    }

    public class CXPLAT_EVENT_3
    {
        private readonly ManualResetEventSlim _event = new ManualResetEventSlim(initialState: false);
        private bool _autoReset = false;

        public CXPLAT_EVENT_3(bool autoReset)
        {
            _autoReset = autoReset;
        }

        public void Signal()
        {
            _event.Set(); // 设置事件为信号状态
        }

        public void Wait()
        {
            _event.Wait(); // 等待事件被设置为信号状态
            if (_autoReset)
            {
                _event.Reset(); // 如果是自动重置，重置事件
            }
        }

        public bool IsSignaled
        {
            get
            {
                return _event.IsSet;
            }
        }
    }

    internal static partial class MSQuicFunc
    {
        static void CxPlatEventInitialize(CXPLAT_EVENT Event, bool ManualReset, bool InitialState)
        {
            Event.AutoReset = !ManualReset;
            Event.Signaled = InitialState;
        }

        static void CxPlatEventUninitialize(CXPLAT_EVENT Event)
        {
            
        }

        static void CxPlatEventSet(CXPLAT_EVENT Event)
        {
            CxPlatInternalEventSet(Event);
        }

        static void CxPlatEventReset(CXPLAT_EVENT Event)
        {
            CxPlatInternalEventReset(Event);
        }

        static void CxPlatInternalEventReset(CXPLAT_EVENT Event)
        {
            Monitor.Enter(Event.Mutex);
            Event.Signaled = false;
            Monitor.Exit(Event.Mutex);
        }

        static void CxPlatInternalEventSet(CXPLAT_EVENT Event)
        {
            Monitor.Enter(Event.Mutex);
            Event.Signaled = true;
            Monitor.PulseAll(Event.Cond);
            Monitor.Exit(Event.Mutex);
        }

        static void CxPlatEventWaitForever(CXPLAT_EVENT Event)
        {
            CxPlatInternalEventWaitForever(Event);
        }

        static void CxPlatInternalEventWaitForever(CXPLAT_EVENT Event)
        {
            Monitor.Enter(Event.Mutex);
            while (!Event.Signaled)
            {
                Monitor.Wait(Event.Cond);
            }

            if (Event.AutoReset)
            {
                Event.Signaled = false;
            }
            Monitor.Exit(Event.Mutex);
        }

        static void CxPlatEventWaitWithTimeout(CXPLAT_EVENT Event, int TimeoutMs)
        {
            Monitor.Enter(Event.Mutex);
            while (!Event.Signaled)
            {
                if(!Monitor.Wait(Event.Cond, TimeoutMs))
                {
                    goto Exit;
                }
            }

            if (Event.AutoReset)
            {
                Event.Signaled = false;
            }
        Exit:
            Monitor.Exit(Event.Mutex);
        }

    }
}
