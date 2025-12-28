using System;

namespace AKNet.Udp4Tcp.Common
{
    internal class Connection : ConnectionPeer,IDisposable
    {
        public bool ConnectAsync(ConnectionEventArgs arg)
        {
            return true;
        }

        public bool DisconnectAsync(ConnectionEventArgs arg)
        {
            return true;
        }


        public bool SendAsync(ConnectionEventArgs arg) 
        {
            arg.LastOperation = ConnectionAsyncOperation.Send;
            arg.ConnectionError = ConnectionError.Success;
            mSendStreamList.WriteFrom(arg.GetSpan());
            return true; 
        }

        public bool ReceiveAsync(ConnectionEventArgs arg)
        {
            return true;
        }


        public void Dispose()
        {
           
        }

        public bool Connected
        {
            get
            {
                return m_Connected;
            }
        }
    }
}
