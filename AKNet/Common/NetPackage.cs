/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    public abstract class NetPackage
    {
        public ushort nPackageId = 0;
        public abstract ReadOnlySpan<byte> GetBuffBody();
        public abstract ReadOnlySpan<byte> GetBuffHead();
        public abstract ReadOnlySpan<byte> GetBuff();
    }
}

