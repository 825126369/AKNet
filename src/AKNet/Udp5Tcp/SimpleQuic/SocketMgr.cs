/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp5Tcp.Common
{
    internal partial class SocketMgr : IDisposable
    {
        public class Config
        {
            public bool bServer;
            public EndPoint mEndPoint;
            public Action<SocketAsyncEventArgs> mReceiveFunc;
        }

        readonly List<SocketItem> mSocketList = new List<SocketItem>();
        public E_LOGIC_RESULT InitNet(Config mConfig)
		{
            try
            {
                int nSocketCount = mConfig.bServer ? AKNet.Udp5Tcp.Common.Config.nSocketCount : 1;
                for (int i = 0; i < nSocketCount; i++)
                {
                    var mSocketItem = new SocketItem(mConfig);
                    mSocketList.Add(mSocketItem);
                    mSocketItem.InitNet();
                }
            }
            catch (Exception ex)
            {
                NetLog.LogError($"SocketMgr 初始化失败: {ex.Message}");
                return E_LOGIC_RESULT.Error;
            }

            return E_LOGIC_RESULT.Success;
        }

        public SocketItem GetSocketItem(int nSocketIndex)
        {
            return mSocketList[nSocketIndex];
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









