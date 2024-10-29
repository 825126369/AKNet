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
        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        private UdpServer mNetServer = null;
        private bool bSendIOContexUsed = false;

        public DisConnectSendMgr(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
        }

        public void SendInnerNetData(UInt16 id, EndPoint removeEndPoint)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpFixedSizePackage mPackage =  UdpNetCommand.GetUdpInnerCommandPackage(id);
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

        private void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            MainThreadCheck.Check();
            mSendPackageQueue.Enqueue(mPackage);

            if (!bSendIOContexUsed)
            {
                bSendIOContexUsed = true;
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