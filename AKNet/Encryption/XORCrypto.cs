using System;
using System.Reflection;
using System.Text;

namespace AKNet.Common
{
    internal class XORCrypto
    {
        readonly byte[] key = Encoding.ASCII.GetBytes("2024/11/23");
        public XORCrypto(string password)
        {
            if (!string.IsNullOrWhiteSpace(password))
            {
                key = Encoding.ASCII.GetBytes(password);
            }
        }

        public byte Encode(int i, byte input, byte token)
        {
            if (i % 2 == 0)
            {
                int nIndex = Math.Abs(i) % key.Length;
                return (byte)(input ^ key[nIndex] ^ token);
            }
            else
            {
                int nIndex = Math.Abs(key.Length - i) % key.Length;
                return (byte)(input ^ key[nIndex] ^ token);
            }
        }
    }
}
