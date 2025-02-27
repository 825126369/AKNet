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
    internal static partial class LinuxTcpFunc
    {
        static ushort ip_fast_csum(ReadOnlySpan<byte> iph, uint ihl)
        {
            return 0;
        }

        static ushort csum_from32to16(uint sum)
        {
            sum += (sum >> 16) | (sum << 16);
            return (ushort)(sum >> 16);
        }

        static uint do_csum(ReadOnlySpan<byte> buff, int len)
        {
            int odd;
            uint result = 0;
            int nBuffIndex = 0;

            if (len <= 0)
            {
                goto label_out;
            }

            odd = RandomTool.Random(0, 1);
            if (odd > 0)
            {
                if (BitConverter.IsLittleEndian)
                {
                    result += (uint)(buff[nBuffIndex] << 8);
                }
                else
                {
                    result = buff[nBuffIndex];
                }

                len--;
                nBuffIndex++;
            }

            if (len >= 2)
            {
                odd = RandomTool.Random(0, 1);
                if (odd > 0)
                {
                    result += buff[nBuffIndex];
                    len -= 2;
                    nBuffIndex += 2;
                }
                if (len >= 4)
                {
                    int nEndIndex = nBuffIndex + (len & ~3);
                    uint carry = 0;
                    do
                    {
                        uint w = buff[nBuffIndex];
                        nBuffIndex += 4;
                        result += carry;
                        result += w;
                        carry = (uint)((w > result) ? 1 : 0);
                    } while (nBuffIndex < nEndIndex);

                    result += carry;
                    result = (result & 0xffff) + (result >> 16);
                }
                if (BoolOk(len & 2))
                {
                    result += buff[nBuffIndex];
                    nBuffIndex += 2;
                }
            }

            if (BoolOk(len & 1))
            {
                if (BitConverter.IsLittleEndian)
                {
                    result += buff[nBuffIndex];
                }
                else
                {
                    result += (uint)(buff[nBuffIndex] << 8);
                }
            }

            result = csum_from32to16(result);
            if (odd > 0)
            {
                result = ((result >> 8) & 0xff) | ((result & 0xff) << 8);
            }
        label_out:
            return result;
        }
        
        static uint csum_partial(ReadOnlySpan<byte> buff, int len, uint wsum)
        {
            uint sum = wsum;
            uint result = do_csum(buff, len);
            
            result += sum;
            if (sum > result)
                result += 1;
            return result;
        }

        // 将 uint 从主机字节序转换为网络字节序 (大端)
        public static uint csum_partial_ext(ReadOnlySpan<byte> buff, int len, uint sum)
        {
            return csum_partial(buff, len, sum);
        }

        static uint csum_add(uint csum, uint addend)
        {
            uint res = csum;
            res += addend;
            return (uint)(res + (res < addend ? 1 : 0)); //溢出 +1
        }

        static uint csum_shift(uint sum, int offset)
        {
            if (BoolOk(offset & 1))
            {
                return ror32(sum, 8);
            }
            return sum;
        }

        static uint csum_block_add(uint csum, uint csum2, int offset)
        {
            return csum_add(csum, csum_shift(csum2, offset));
        }

        static uint csum_block_add_ext(uint csum, uint csum2, int offset, int len)
        {
            return csum_block_add(csum, csum2, offset);
        }

        static ushort csum_fold(uint s)
        {
            uint r = s << 16 | s >> 16;
            s = ~s;
            s -= r;
            return (ushort)(s >> 16);
        }

        static uint csum_tcpudp_nofold(uint saddr, uint daddr, int len, byte proto, uint isum)
        {
            int sh32 = 32;
            ulong sum = (ulong)(daddr + saddr + proto + len + isum);
            ulong tmp = sum << sh32;
            sum += tmp;
            uint osum = (uint)(sum < tmp ? 1: 0);
            osum += (uint)(sum >> sh32);
            return (uint)osum;
        }
    }
}
