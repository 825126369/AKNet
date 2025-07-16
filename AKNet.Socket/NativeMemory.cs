using System.Runtime.CompilerServices;

namespace AKNet.Socket
{
    public static unsafe partial class NativeMemory
    {
        public static bool IsPow2(int value) => (value & (value - 1)) == 0 && value > 0;
        public static bool IsPow2(nuint value) => (value & (value - 1)) == 0 && value > 0;

        //[CLSCompliant(false)]
        //public static void* Alloc(nuint elementCount, nuint elementSize)
        //{
        //    nuint byteCount = GetByteCount(elementCount, elementSize);
        //    return Alloc(byteCount);
        //}

        //[CLSCompliant(false)]
        //public static void* AllocZeroed(nuint byteCount)
        //{
        //    return AllocZeroed(byteCount, elementSize: 1);
        //}

        ///// <summary>Clears a block of memory.</summary>
        ///// <param name="ptr">A pointer to the block of memory that should be cleared.</param>
        ///// <param name="byteCount">The size, in bytes, of the block to clear.</param>
        ///// <remarks>
        /////     <para>If this method is called with <paramref name="ptr" /> being <see langword="null"/> and <paramref name="byteCount" /> being <c>0</c>, it will be equivalent to a no-op.</para>
        /////     <para>The behavior when <paramref name="ptr" /> is <see langword="null"/> and <paramref name="byteCount" /> is greater than <c>0</c> is undefined.</para>
        ///// </remarks>
        //[CLSCompliant(false)]
        //public static unsafe void Clear(void* ptr, nuint byteCount)
        //{
        //    SpanHelpers.ClearWithoutReferences(ref *(byte*)ptr, byteCount);
        //}

        ///// <summary>
        ///// Copies a block of memory from memory location <paramref name="source"/>
        ///// to memory location <paramref name="destination"/>.
        ///// </summary>
        ///// <param name="source">A pointer to the source of data to be copied.</param>
        ///// <param name="destination">A pointer to the destination memory block where the data is to be copied.</param>
        ///// <param name="byteCount">The size, in bytes, to be copied from the source location to the destination.</param>
        //[CLSCompliant(false)]
        //public static void Copy(void* source, void* destination, nuint byteCount)
        //{
        //    SpanHelpers.Memmove(ref *(byte*)destination, ref *(byte*)source, byteCount);
        //}

        ///// <summary>
        ///// Copies the byte <paramref name="value"/> to the first <paramref name="byteCount"/> bytes
        ///// of the memory located at <paramref name="ptr"/>.
        ///// </summary>
        ///// <param name="ptr">A pointer to the block of memory to fill.</param>
        ///// <param name="byteCount">The number of bytes to be set to <paramref name="value"/>.</param>
        ///// <param name="value">The value to be set.</param>
        //[CLSCompliant(false)]
        //public static void Fill(void* ptr, nuint byteCount, byte value)
        //{
        //    SpanHelpers.Fill(ref *(byte*)ptr, byteCount, value);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static nuint GetByteCount(nuint elementCount, nuint elementSize)
        //{
        //    // This is based on the `mi_count_size_overflow` and `mi_mul_overflow` methods from microsoft/mimalloc.
        //    // Original source is Copyright (c) 2019 Microsoft Corporation, Daan Leijen. Licensed under the MIT license

        //    // sqrt(nuint.MaxValue)
        //    nuint multiplyNoOverflow = (nuint)1 << (4 * sizeof(nuint));

        //    return ((elementSize >= multiplyNoOverflow) || (elementCount >= multiplyNoOverflow)) && (elementSize > 0) && ((nuint.MaxValue / elementSize) < elementCount) ? nuint.MaxValue : (elementCount * elementSize);
        //}
    }
}
