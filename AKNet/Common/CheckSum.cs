using System;

namespace AKNet.Common
{
    public static class CheckSumHelper
    {
        /// <summary>
        /// 计算部分数据的校验和，并结合之前的校验和。
        /// </summary>
        /// <param name="buff">数据缓冲区。</param>
        /// <param name="len">数据长度（字节）。</param>
        /// <param name="wsum">之前的校验和。</param>
        /// <returns>新的校验和。</returns>
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

        /// <summary>
        /// 计算整个数据缓冲区的校验和。
        /// </summary>
        /// <param name="buff">数据缓冲区。</param>
        /// <param name="len">数据长度（字节）。</param>
        /// <returns>校验和。</returns>
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

        /// <summary>
        /// 将 32 位校验和压缩到 16 位，并处理字节序。
        /// </summary>
        /// <param name="sum">32 位校验和。</param>
        /// <returns>16 位校验和。</returns>
        private static uint CsumFrom32To16(uint sum)
        {
            while (sum >> 16 != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }
            return sum;
        }
    }
}