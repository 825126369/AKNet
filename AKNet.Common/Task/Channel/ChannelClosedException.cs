using System;

namespace AKNet.Common.Channel
{
    public partial class ChannelClosedException : InvalidOperationException
    {
        public ChannelClosedException() :
            base() { }

        public ChannelClosedException(string? message) : base(message) { }
        public ChannelClosedException(Exception? innerException) : base() { }
        public ChannelClosedException(string? message, Exception? innerException) : base(message) { }
    }
}
