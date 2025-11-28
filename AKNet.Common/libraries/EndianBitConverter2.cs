/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
[assembly: InternalsVisibleTo("AKNet.Other")]
namespace AKNet.Common
{
    //这里默认使用大端存储的
    internal static class EndianBitConverter2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, ulong value)
        {
            BinaryPrimitives.WriteUInt64BigEndian(mBuffer.Slice(nBeginIndex), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, int value)
        {
            BinaryPrimitives.WriteInt32BigEndian(mBuffer.Slice(nBeginIndex), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(mBuffer.Slice(nBeginIndex), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(mBuffer.Slice(nBeginIndex), value);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, int value)
        {
            BinaryPrimitives.WriteInt32BigEndian(mBuffer.AsSpan().Slice(nBeginIndex), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(mBuffer.AsSpan().Slice(nBeginIndex), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(mBuffer.AsSpan().Slice(nBeginIndex), value);
        }
        
        //--------------------------------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(byte[] mBuffer, int nBeginIndex)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(mBuffer.AsSpan().Slice(nBeginIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte[] mBuffer, int nBeginIndex)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(mBuffer.AsSpan().Slice(nBeginIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(byte[] mBuffer, int nBeginIndex)
        {
            return BinaryPrimitives.ReadInt32BigEndian(mBuffer.AsSpan().Slice(nBeginIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(byte[] mBuffer, int nBeginIndex)
        {
            return BinaryPrimitives.ReadUInt64BigEndian(mBuffer.AsSpan().Slice(nBeginIndex));
        }
        


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(mBuffer.Slice(nBeginIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return BinaryPrimitives.ReadInt32BigEndian(mBuffer.Slice(nBeginIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(mBuffer.Slice(nBeginIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return BinaryPrimitives.ReadUInt64BigEndian(mBuffer.Slice(nBeginIndex));
        }

    }
}
