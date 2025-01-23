using AKNet.Common;
using System;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        static ushort ip_fast_csum(ReadOnlySpan<byte> iph, uint ihl)
        {
            return 0;
        }

        //折叠校验和：将 32 位校验和折叠为 16 位校验和。
        //为什么这样可以折叠校验和
        //保留校验信息：
        //通过将高 16 位和低 16 位相加，确保了 32 位值中的所有信息都被考虑在内。即使高 16 位和低 16 位的值较大，相加后可能会产生进位，这个进位也会被保留下来。
        //确保 16 位结果：
        //最终通过右移 16 位，将 32 位值的高 16 位提取出来，确保结果是一个 16 位的值。这一步确保了最终结果不会超过 16 位的范围。
        //数学原理：
        //校验和的计算通常基于模运算。通过将 32 位值折叠成 16 位值，实际上是在进行模 216 的运算。这确保了结果在 16 位范围内，同时保留了校验信息。
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
            ulong sum = daddr;
            ulong tmp;
            uint osum;

            tmp = (ulong)saddr;
            sum += tmp;
            
            tmp = (uint)(proto + len);
            if (BitConverter.IsLittleEndian) 
            {
                tmp <<= 8;
            }

            sum += tmp;

            tmp = (ulong)isum;
            sum += tmp;

            
            tmp = sum << sh32;
            sum += tmp;
            osum = (uint)(sum < tmp ? 1: 0);
            osum += (uint)(sum >> sh32);

            return (uint)osum;
        }
    }
}
