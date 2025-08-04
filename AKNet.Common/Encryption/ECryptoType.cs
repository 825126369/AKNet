/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
namespace AKNet.Common
{
    internal enum ECryptoType
    {
        None = 0,
        Xor = 1,
    }

    internal static class ECryptoConfig
    {
        public const ECryptoType nECryptoType = ECryptoType.None;
    }
}
