/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:02
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using AKNet.MSQuic.Common;
using System.Net;
using System.Net.Security;

namespace AKNet.MSQuic.Server
{
    internal partial class ServerMgr
    {
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
            InitNet(IPAddress.Any, nPort);
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
                NetLog.Log("服务器 初始化成功: " + mIPAddress + " | " + nPort);
                StartProcessAccept();
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
            mOption.GetConnectionOptionFunc = ConnectionOptionsCallback;
            return mOption;
        }

        private QuicConnectionOptions ConnectionOptionsCallback()
        {
            var mCert = X509CertTool.GetPfxCert();

            //mCert = X509CertificateLoader.LoadCertificateFromFile("D:\\Me\\OpenSource\\AKNet2\\cert.pfx");
            NetLog.Assert(mCert != null, "GetCert() == null");

            var ApplicationProtocols = new List<SslApplicationProtocol>();
            ApplicationProtocols.Add(SslApplicationProtocol.Http11);
            ApplicationProtocols.Add(SslApplicationProtocol.Http2);
            ApplicationProtocols.Add(SslApplicationProtocol.Http3);

            var ServerAuthenticationOptions = new SslServerAuthenticationOptions();
            ServerAuthenticationOptions.ApplicationProtocols = ApplicationProtocols;
            ServerAuthenticationOptions.ServerCertificate = mCert;

            QuicConnectionOptions mOption = new QuicConnectionOptions();
            mOption.ServerAuthenticationOptions = ServerAuthenticationOptions;
            return mOption;
        }

        private async void StartProcessAccept()
        {
            while (mQuicListener != null)
            {
                try
                {
                    QuicConnection connection = await mQuicListener.AcceptConnectionAsync();
                    MultiThreadingHandleConnectedSocket(connection);
                }
                catch (Exception e)
                {
                    NetLog.LogError(e.ToString());
                }
            }
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
