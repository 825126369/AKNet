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

namespace AKNet.Udp4Tcp.Common
{
    internal partial class SocketMgr : IDisposable
    {
        public class Config
        {
            public bool bServer;
            public EndPoint mEndPoint;
        }

        readonly List<SocketItem> mSocketList = new List<SocketItem();
        public int InitNet(Config mConfig)
		{
            try
            {
                IPAddress mIPAddress = IPAddress.Parse(mConfig.IP);
                EndPoint mIPEndPoint = new IPEndPoint(mIPAddress, mConfig.nPort);

                int nSocketCount = mConfig.bServer ? Environment.ProcessorCount : 1;
                for (int i = 0; i < nSocketCount; i++)
                {
                    var mSocketItem = new SocketItem();
                    mSocketList.Add(mSocketItem);
                    mSocketItem.RemoteEndPoint = mIPEndPoint as IPEndPoint;

                    if (mConfig.bServer)
                    {
                        mSocketItem.mSocket.Bind(mIPEndPoint);
                    }
                    else
                    {
                        mSocketItem.mSocket.Connect(mIPEndPoint);
                    }

                    mSocketItem.StartReceiveFromAsync();
                }

                if(mConfig.bServer)
                {
                    NetLog.Log("Udp Server 初始化成功:  " + mIPEndPoint.ToString());
                }
                else
                {
                    NetLog.Log("Udp Client 连接服务器成功:  " + mIPEndPoint.ToString());
                }
            }
            catch (Exception ex)
            {
                NetLog.LogError($"服务器 初始化失败: {ex.Message}");
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
            mSocketList.Clear();
        }
    }

}









