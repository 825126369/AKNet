/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    //TCP 校验和采用 16 位的反码求和算法
    //1: 拼接数据：伪头部（源 IP、目的 IP、协议号、TCP 长度）
    //2:TCP 头部数据部分按 16 位分组并求和：
    //3:将拼接后的数据按每 2 字节一组，进行累加。
    //4:处理进位：如果累加结果超过 16 位，将进位加回到结果的低位。
    //5:取反得到校验和：将最终的累加结果取反，存储在校验和字段中。
    public static class CheckSumHelper
    {
        // 计算部分数据的校验和，并结合之前的校验和。
        public static ushort CsumPartial(byte[] buff, int len, ushort wsum)
        {
            uint sum = wsum;
            uint result = DoCsum(buff, len);

            // 加入旧的校验和，并处理进位
            result += sum;
            if (sum > result)
            {
                result += 1;
            }

            return (ushort)result;
        }

        // 计算整个数据缓冲区的校验和。
        public static ushort DoCsum(byte[] buff, int len)
        {
            if (buff == null || len <= 0)
            {
                return 0;
            }

            uint result = 0;
            int index = 0;

            // 处理 4 字节对齐的数据块
            for (index = 0; index + 4 <= len; index += 4)
            {
                uint w = EndianBitConverter.ToUInt32(buff, index);
                result += w;
                if (w > result)
                {
                    result += 1;
                }
            }

            // 处理 2 字节对齐的数据块
            for (index = 0; index + 2 <= len; index += 2)
            {
                uint w = EndianBitConverter.ToUInt16(buff, index);
                result += w;
                if (w > result)
                {
                    result += 1;
                }
            }

            NetLog.Assert(index == len - 1);
            if (index == len - 1)
            {
                uint w = buff[index];
                result += w;
                if (w > result)
                {
                    result += 1;
                }
                index++;
            }
            
            // 将结果压缩到 16 位，并处理字节序
            result = CsumFrom32To16(result);
            return (ushort)result;
        }

        //将32位校验和压缩到 16 位，并处理字节序。
        private static ushort CsumFrom32To16(uint sum)
        {
            if (sum > ushort.MaxValue)
            {
                // 将高位2个字节和低位两个字节相加
                sum = (sum & 0xFFFF) + (sum >> 16);
            }
            return (ushort)sum;
        }

        //将64位值折叠为 32 位校验和。
        private static uint CSumFrom64To32(ulong s)
        {
            if (s > uint.MaxValue)
            {
                // 如果高 32 位不为零，则将其加到低 32 位
                s = (s & 0xFFFFFFFF) + (s >> 32);
            }
            return (uint)s;
        }
            
        private static ushort CSumFrom64To16(ulong s)
        {
            return CsumFrom32To16(CSumFrom64To32(s));
        }

        public static ushort CsumTcpFakeHead(uint saddr, uint daddr, int len, byte proto)
        {
            // 将所有部分相加
            ulong s = 0;
            s += saddr;
            s += daddr;
            s += proto;
            return CSumFrom64To16(s);
        }

        // 计算 TCP 或 UDP 校验和（完整版本）。
        public static ushort ComputeTcpUdpChecksum(byte[] buff, int len, uint saddr, uint daddr, byte proto)
        {
            // 计算部分校验和
            ushort partialSum = CsumTcpFakeHead(saddr, daddr, len, proto);
            // 合并两部分校验和
            return CsumPartial(buff, len, partialSum);
        }
    }
}