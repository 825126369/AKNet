/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class InnerCommandSendMgr
    {
        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        readonly SafeObjectPool<NetUdpFixedSizePackage> mPackagePool = new SafeObjectPool<NetUdpFixedSizePackage>();
        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();

        private readonly UdpServer mNetServer = null;
        private bool bSendIOContexUsed = false;

        public InnerCommandSendMgr(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
        }

        public void SendInnerNetData(ushort nId, EndPoint removeEndPoint)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(nId));

            NetUdpFixedSizePackage mPackage = mPackagePool.Pop();
            mPackage.nPackageId = nId;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            mPackage.remoteEndPoint = removeEndPoint;
            UdpPackageEncryption.Encode(mPackage);
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
            if (Config.bUseSendAsync)
            {
                mSendPackageQueue.Enqueue(mPackage);

                if (!bSendIOContexUsed)
                {
                    bSendIOContexUsed = true;
                    SendNetPackage2(SendArgs);
                }
            }
            else
            {
                mNetServer.GetSocketMgr().SendTo(mPackage);
                mPackagePool.recycle(mPackage);
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
                mPackagePool.recycle(mPackage);

                if (!mNetServer.GetSocketMgr().SendToAsync(e))
                {
                    ProcessSend(null, e);
                }
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }
    }
}