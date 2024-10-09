using System.Collections.Generic;

namespace XKNet.Common
{
    internal class IdManager
    {
        private Queue<uint> mStackPool = null;
        private uint nIdGenerator = 0;

        public IdManager(uint nInitId = 1)
        {
            nIdGenerator = nInitId;
            mStackPool = new Queue<uint>();
        }

        public uint Pop()
        {
            if (mStackPool.Count > 0)
            {
                return mStackPool.Dequeue();
            }
            else
            {
                return nIdGenerator++;
            }
        }

        public void Recycle(uint nId)
        {
#if DEBUG 
            NetLog.Assert(!mStackPool.Contains(nId), nId);
#endif

            mStackPool.Enqueue(nId);
        }
    }

    internal class SafeIdManager
    {
        private Queue<uint> mStackPool = null;
        private uint nIdGenerator = 0;

        public SafeIdManager(uint nInitId = 1)
        {
            nIdGenerator = nInitId;
            mStackPool = new Queue<uint>();
        }

        public uint Pop()
        {
            lock (mStackPool)
            {
                if (mStackPool.Count > 0)
                {
                    return mStackPool.Dequeue();
                }
                else
                {
                    return nIdGenerator++;
                }
            }
        }

        public void Recycle(uint nId)
        {
            lock (mStackPool)
            {
#if DEBUG
                NetLog.Assert(!mStackPool.Contains(nId), nId);
#endif

                mStackPool.Enqueue(nId);
            }
        }
    }
}
