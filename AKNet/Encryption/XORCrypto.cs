using System;
using System.Text;

namespace AKNet.Common
{
    internal class XORCrypto
    {
        readonly byte[] key = null;
        public XORCrypto(string password)
        {
            key = Encoding.ASCII.GetBytes(password);
        }

        public byte Encode(int i, byte input)
        {
            return (byte)(input ^ key[i % key.Length]);
        }
    }
}
