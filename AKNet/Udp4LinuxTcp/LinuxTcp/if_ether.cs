/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class ethhdr
    {
        public const int ETH_ALEN = 6;		/* Octets in one ethernet addr	 */

        public byte[] h_dest = new byte[ETH_ALEN]; //目的 MAC 地址
        public byte[] h_source = new byte[ETH_ALEN]; //源 MAC 地址
        public ushort h_proto;     //协议类型字段

        public void WriteTo(Span<byte> mBuffer)
        {
            h_dest.CopyTo(mBuffer);
            h_source.CopyTo(mBuffer);
            EndianBitConverter.SetBytes(mBuffer, 12, h_proto);
        }

        public void WriteFrom(ReadOnlySpan<byte> mBuffer)
        {
            h_dest = mBuffer.Slice(0, ETH_ALEN).ToArray();
            h_source = mBuffer.Slice(ETH_ALEN, ETH_ALEN).ToArray();
            h_proto = EndianBitConverter.ToUInt16(mBuffer, 12);
        }

    }

    internal static partial class LinuxTcpFunc
    {
      
    }

}
