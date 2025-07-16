// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace AKNet.Socket
{
    public interface IParsable<TSelf> where TSelf : IParsable<TSelf>?
    {
         TSelf Parse(string s, IFormatProvider? provider);
         bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(returnValue: false)] out TSelf result);
    }
}
