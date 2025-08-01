using System;
using System.Runtime.Serialization;

namespace AKNet.Common.Channel
{
    [Serializable]
    public partial class ChannelClosedException : InvalidOperationException
    {
        protected ChannelClosedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
