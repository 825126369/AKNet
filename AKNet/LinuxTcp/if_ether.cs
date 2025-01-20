namespace AKNet.LinuxTcp
{
    internal class ethhdr
    {
        public const int ETH_ALEN = 6;		/* Octets in one ethernet addr	 */

        public byte[] h_dest = new byte[ETH_ALEN]; //目的 MAC 地址
        public byte[] h_source = new byte[ETH_ALEN]; //源 MAC 地址
        public ushort h_proto;     //协议类型字段
    }
}
