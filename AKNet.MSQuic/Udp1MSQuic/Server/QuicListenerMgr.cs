/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1MSQuic.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;

namespace AKNet.Udp1MSQuic.Server
{
    internal class QuicListenerMgr
    {
        QuicListener mQuicListener = null;
        QuicServer mQuicServer = null;
        private SOCKET_SERVER_STATE mState = SOCKET_SERVER_STATE.NONE;
        private int nPort;

        public QuicListenerMgr(QuicServer mQuicServer)
        {
            this.mQuicServer = mQuicServer;

        }

        public void InitNet()
        {
            List<int> mPortList = IPAddressHelper.GetAvailableTcpPortList();
            int nTryBindCount = 100;
            while (nTryBindCount-- > 0)
            {
                if (mPortList.Count > 0)
                {
                    int nPort = mPortList[RandomTool.RandomArrayIndex(0, mPortList.Count)];
                    InitNet(nPort);
                    mPortList.Remove(nPort);
                    if (GetServerState() == SOCKET_SERVER_STATE.NORMAL)
                    {
                        break;
                    }
                }
            }

            if (GetServerState() != SOCKET_SERVER_STATE.NORMAL)
            {
                NetLog.LogError("Udp Server 自动查找可用端口 失败！！！");
            }
        }

        public void InitNet(int nPort)
        {
            InitNet(IPAddress.IPv6Any, nPort);
        }

        public void InitNet(string Ip, int nPort)
        {
            InitNet(IPAddress.Parse(Ip), nPort);
        }

        private void InitNet(IPAddress mIPAddress, int nPort)
        {
            this.nPort = nPort;
            this.mState = SOCKET_SERVER_STATE.NORMAL;

            try
            {
                var options = GetQuicListenerOptions(mIPAddress, nPort);
                mQuicListener = QuicListener.StartListen(options);
                if (mQuicListener != null)
                {
                    NetLog.Log("服务器 初始化成功: " + mIPAddress + " | " + nPort);
                }
            }
            catch (Exception e)
            {
                this.mState = SOCKET_SERVER_STATE.EXCEPTION;
                NetLog.LogError(e.ToString());
            }
        }

        private QuicListenerOptions GetQuicListenerOptions(IPAddress mIPAddress, int nPort)
        {
            QuicListenerOptions mOption = new QuicListenerOptions();
            mOption.ListenEndPoint = new IPEndPoint(mIPAddress, nPort);
            mOption.GetConnectionOptionFunc = GetConnectionOptionFunc;
            mOption.AcceptConnectionFunc = AcceptConnectionFunc;
            return mOption;
        }

        private QuicConnectionOptions GetConnectionOptionFunc()
        {
            var mCert = X509CertTool.GetPfxCert();
            //mCert = X509CertificateLoader.LoadCertificateFromFile("D:\\Me\\OpenSource\\AKNet2\\cert.pfx");

            NetLog.Assert(mCert != null, "GetCert() == null");
            var ServerAuthenticationOptions = new SslServerAuthenticationOptions();
            ServerAuthenticationOptions.ServerCertificate = mCert;

            QuicConnectionOptions mOption = new QuicConnectionOptions();
            mOption.ServerAuthenticationOptions = ServerAuthenticationOptions;
            return mOption;
        }

        private void AcceptConnectionFunc(QuicConnection connection)
        {
            mQuicServer.mClientPeerManager.MultiThreadingHandleConnectedSocket(connection);
        }

        public int GetPort()
        {
            return this.nPort;
        }

        public SOCKET_SERVER_STATE GetServerState()
        {
            return mState;
        }

        public void CloseNet()
        {
            MainThreadCheck.Check();
            if (mQuicListener != null)
            {
                var mQuicListener2 = mQuicListener;
                mQuicListener = null;
                mQuicListener2.Close();
            }
        }

    }

}
