/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class SocketMgr:IDisposable
    {
        public class Config
        {
            public bool bServer;
            public string IP;
            public int nPort;
        }

        readonly List<SocketItem> mSocketList = new List<SocketItem();

        private int InitNet(Config mConfig)
		{
            try
            {
                IPAddress mIPAddress = IPAddress.Parse(mConfig.IP);
                EndPoint mIPEndPoint = new IPEndPoint(mIPAddress, mConfig.nPort);
                for (int i = 0; i < mSocketList.Count; i++)
                {
                    var mSocketItem = new SocketItem();
                    if (mConfig.bServer)
                    {
                        mSocketItem.mSocket.Bind(mIPEndPoint);
                        NetLog.Log("Udp Server 初始化成功:  " + mIPEndPoint.ToString());
                    }
                    else
                    {
                        mSocketItem.mSocket.Connect(mIPEndPoint);
                        NetLog.Log("Udp Client 初始化成功:  " + mIPEndPoint.ToString());
                    }

                    mSocketItem.StartReceiveFromAsync();
                }
            }
            catch (Exception ex)
            {
                NetLog.LogError(ex.Message + " | " + ex.StackTrace);
                NetLog.LogError("服务器 初始化失败: " + mIPAddress + " | " + nPort);
                return 1;
            }

            return 0;
        }

        public void Dispose()
        {
            for (int i = 0; i < mSocketList.Count; i++)
            {
                mSocketList[i].Dispose();
            }
        }
    }

}









