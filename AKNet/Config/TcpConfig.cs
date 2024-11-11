using AKNet.Tcp.Common;
using System;

namespace AKNet.Common
{
    public interface TcpNetPackageEncryptionInterface
    {
        bool DeEncryption(AkCircularBuffer<byte> mReceiveStreamList, TcpNetPackage mPackage);
        ReadOnlySpan<byte> Encryption(int nPackageId, ReadOnlySpan<byte> mBufferSegment);
    }

    public class TcpConfig
    {
        public int nMsgPackageBufferMaxLength = 1024 * 8;
        public double fSendHeartBeatMaxTimeOut = 2.0;
        public double fReceiveHeartBeatMaxTimeOut = 5.0;
        public double fReceiveReConnectMaxTimeOut = 3.0;
        public int numConnections = 10000;
        public TcpNetPackageEncryptionInterface NetPackageEncryptionInterface = null;
    }
}
