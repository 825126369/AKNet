// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AKNet.Platform.Socket
{
    /// <devdoc>
    ///    <para>
    ///       Identifies a network address.
    ///    </para>
    /// </devdoc>
    public abstract class EndPoint
    {
        /// <devdoc>
        ///    <para>
        ///       Returns the Address Family to which the EndPoint belongs.
        ///    </para>
        /// </devdoc>
        public virtual AddressFamily AddressFamily
        {
            get
            {
                throw new Exception();
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Serializes EndPoint information into a SocketAddress structure.
        ///    </para>
        /// </devdoc>
        public virtual SocketAddress Serialize()
        {
            throw new Exception();
        }

        /// <devdoc>
        ///    <para>
        ///       Creates an EndPoint instance from a SocketAddress structure.
        ///    </para>
        /// </devdoc>
        public virtual EndPoint Create(SocketAddress socketAddress)
        {
            throw new Exception();
        }
    }
}
