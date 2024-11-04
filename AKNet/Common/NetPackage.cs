/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
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

