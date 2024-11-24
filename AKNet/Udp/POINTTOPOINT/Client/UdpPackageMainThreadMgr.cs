/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    internal class UdpPackageMainThreadMgr
    {
        private readonly Queue<NetUdpFixedSizePackage> mPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private ClientPeer mClientPeer = null;

		public UdpPackageMainThreadMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
        }

        public void Update(double elapsed)
        {
            while(NetPackageExecute())
            {

            }
        }

        private bool NetPackageExecute()
        {
            NetUdpFixedSizePackage mPackage = null;
            lock (mPackageQueue)
            {
                mPackageQueue.TryDequeue(out mPackage);
            }

            if (mPackage != null)
            {
                UdpStatistical.AddReceivePackageCount();
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }

            return false;
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            if (Config.bSocketSendMultiPackage)
            {
                var mBuff = new ReadOnlySpan<byte>(e.Buffer, e.Offset, e.BytesTransferred);
                while (true)
                {
                    var mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                    bool bSucccess = mClientPeer.GetCryptoMgr().Decode(mBuff, mPackage);
                    if (bSucccess)
                    {
                        int nReadBytesCount = mPackage.Length;

                        lock (mPackageQueue)
                        {
                            mPackageQueue.Enqueue(mPackage);
                        }

                        if (mBuff.Length > nReadBytesCount)
                        {
                            mBuff = mBuff.Slice(nReadBytesCount);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                        NetLog.LogError("解码失败 !!!");
                        break;
                    }
                }
            }
            else
            {
                NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                Buffer.BlockCopy(e.Buffer, e.Offset, mPackage.buffer, 0, e.BytesTransferred);
                mPackage.Length = e.BytesTransferred;
                bool bSucccess = mClientPeer.GetCryptoMgr().Decode(mPackage);
                if (bSucccess)
                {
                    mPackageQueue.Enqueue(mPackage);
                }
                else
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                    NetLog.LogError("解码失败 !!!");
                }
            }
        }
    }
}