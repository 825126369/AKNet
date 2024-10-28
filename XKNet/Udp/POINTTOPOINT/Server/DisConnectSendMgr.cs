using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class DisConnectSendMgr
    {
        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        private UdpServer mNetServer = null;
        public DisConnectSendMgr(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
        }

        private NetUdpFixedSizePackage GetUdpInnerCommandPackage(UInt16 id)
        {
            NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
            mPackage.nOrderId = 0;
            mPackage.nGroupCount = 0;
            mPackage.nPackageId = id;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            NetPackageEncryption.Encryption(mPackage);
            return mPackage;
        }

        public void SendInnerNetData(UInt16 id, EndPoint removeEndPoint)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpFixedSizePackage mPackage = GetUdpInnerCommandPackage(id);
            mPackage.remoteEndPoint = removeEndPoint;
            SendNetPackage(mPackage);
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetPackage2(e);
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        readonly object bSendIOContexUsedObj = new object();
        bool bSendIOContexUsed = false;
        private void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            mSendPackageQueue.Enqueue(mPackage);

            bool bCanGoNext = false;
            lock (bSendIOContexUsedObj)
            {
                bCanGoNext = bSendIOContexUsed == false;
                if (!bSendIOContexUsed)
                {
                    bSendIOContexUsed = true;
                }
            }

            if (bCanGoNext)
            {
                SendNetPackage2(SendArgs);
            }
        }

        private void SendNetPackage2(SocketAsyncEventArgs e)
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.TryDequeue(out mPackage))
            {
                Array.Copy(mPackage.buffer, e.Buffer, mPackage.Length);
                e.SetBuffer(0, mPackage.Length);
                e.RemoteEndPoint = mPackage.remoteEndPoint;
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
                mNetServer.mSocketMgr.SendNetPackage(e, ProcessSend);
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }
    }
}