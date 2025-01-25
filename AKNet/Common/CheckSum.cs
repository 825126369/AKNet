using System;

namespace AKNet.Common
{
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
                return 0;

            int odd = (buff.Length & 1) == 1 ? 1 : 0;
            uint result = 0;
            int index = 0;

            // 如果起始地址为奇数，处理第一个字节
            if (odd != 0)
            {
                result += (uint)(buff[0] << 8);
                index++;
                len--;
            }

            // 处理剩余的 2 字节对齐的数据
            if (len >= 2)
            {
                if ((index & 1) != 0)
                {
                    result += BitConverter.ToUInt16(buff, index);
                    index += 2;
                    len -= 2;
                }

                // 处理 4 字节对齐的数据块
                if (len >= 4)
                {
                    int end = index + (len & ~3);
                    uint carry = 0;
                    while (index < end)
                    {
                        uint w = BitConverter.ToUInt32(buff, index);
                        index += 4;
                        result += carry;
                        result += w;
                        carry = (uint)(w > result ? 1 : 0);
                    }
                    result += carry;
                    result = (result & 0xFFFF) + (result >> 16);
                }

                // 处理剩余的 2 字节
                if ((len & 2) != 0)
                {
                    result += BitConverter.ToUInt16(buff, index);
                    index += 2;
                }
            }

            // 处理剩余的 1 字节
            if ((len & 1) != 0)
            {
                result += buff[index];
            }

            // 将结果压缩到 16 位，并处理字节序
            result = CsumFrom32To16(result);
            if (odd != 0)
            {
                result = ((result >> 8) & 0xFF) | ((result & 0xFF) << 8);
            }

            return (ushort)result;
        }

        //将32位校验和压缩到 16 位，并处理字节序。
        private static uint CsumFrom32To16(uint sum)
        {
            while (sum >> 16 != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }
            return sum;
        }

        //将64位值折叠为 32 位校验和。
        private static uint From64To32(ulong s)
        {
            // 如果高 32 位不为零，则将其加到低 32 位
            if ((s >> 32) != 0)
            {
                s = (s & 0xFFFFFFFF) + (s >> 32);
            }
            // 如果结果超过 16 位，则再次折叠
            if ((s >> 16) != 0)
            {
                s = (s & 0xFFFF) + (s >> 16);
            }
            return (uint)s;
        }

        public static uint CsumTcpUdpNofold(uint saddr, uint daddr, uint len, byte proto, uint sum)
        {
            // 将所有部分相加
            ulong s = sum;

            s += saddr;
            s += daddr;

            // 根据字节序添加协议类型和长度
            if (BitConverter.IsLittleEndian)
            {
                s += proto + len;
            }
            else
            {
                s += (proto + len) << 8;
            }

            // 返回未折叠的 32 位结果
            return (uint)s;
        }

        // 计算 TCP 或 UDP 校验和（完整版本）。
        public static ushort ComputeTcpUdpChecksum(byte[] buff, int len, uint saddr, uint daddr, byte proto)
        {
            // 计算部分校验和
            uint partialSum = CsumTcpUdpNofold(saddr, daddr, len, proto, 0);

            // 计算数据部分的校验和
            uint dataSum = DoCsum(buff, len);

            // 合并两部分校验和
            uint totalSum = partialSum + dataSum;

            // 折叠为 16 位校验和
            return (ushort)From64To32(totalSum);
        }
    }
}