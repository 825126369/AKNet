namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_CONN_TIMER_TYPE
    {
        QUIC_CONN_TIMER_PACING,
        QUIC_CONN_TIMER_ACK_DELAY,
        QUIC_CONN_TIMER_LOSS_DETECTION,
        QUIC_CONN_TIMER_KEEP_ALIVE,
        QUIC_CONN_TIMER_IDLE,
        QUIC_CONN_TIMER_SHUTDOWN,
        QUIC_CONN_TIMER_COUNT
    }

    internal enum QUIC_OPERATION_TYPE
    {
        QUIC_OPER_TYPE_API_CALL,            // Process an API call from the app.
        QUIC_OPER_TYPE_FLUSH_RECV,          // Process queue of receive packets.
        QUIC_OPER_TYPE_UNREACHABLE,         // Process UDP unreachable event.
        QUIC_OPER_TYPE_FLUSH_STREAM_RECV,   // Indicate a stream data to the app.
        QUIC_OPER_TYPE_FLUSH_SEND,          // Frame packets and send them.
        QUIC_OPER_TYPE_DEPRECATED,          // No longer used.
        QUIC_OPER_TYPE_TIMER_EXPIRED,       // A timer expired.
        QUIC_OPER_TYPE_TRACE_RUNDOWN,       // A trace rundown was triggered.
        QUIC_OPER_TYPE_ROUTE_COMPLETION,    // Process route completion event.
        QUIC_OPER_TYPE_VERSION_NEGOTIATION, // A version negotiation needs to be sent.
        QUIC_OPER_TYPE_STATELESS_RESET,     // A stateless reset needs to be sent.
        QUIC_OPER_TYPE_RETRY,               // A retry needs to be sent.
    }

    internal class QUIC_OPERATION
    {
        public quic_platform_cxplat_list_entry Link;
        public QUIC_OPERATION_TYPE Type;
        public bool FreeAfterProcess;
        
        //struct INITIALIZE_Class
        //{
        //    void* Reserved; // Nothing.
        //}
        //public INITIALIZE_Class INITIALIZE;

        //struct API_CALL_Class
        //{
        //    QUIC_API_CONTEXT* Context;
        //}
        //public API_CALL_Class API_CALL;

        //struct 
        //{
        //    void* Reserved; // Nothing.
        //}
        //FLUSH_RECEIVE;
        //struct {
        //    QUIC_ADDR RemoteAddress;
        //}
        //UNREACHABLE;
        //struct {
        //    QUIC_STREAM* Stream;
        //}
        //FLUSH_STREAM_RECEIVE;
        //struct {
        //    void* Reserved; // Nothing.
        //}
        //FLUSH_SEND;
        //struct {
        //    QUIC_CONN_TIMER_TYPE Type;
        //}
        //TIMER_EXPIRED;
        //struct {
        //    QUIC_STATELESS_CONTEXT* Context;
        //}
        //STATELESS; // Stateless reset, retry and VN
        //struct {
        //    uint8_t PhysicalAddress[6];
        //    uint8_t PathId;
        //    BOOLEAN Succeeded;
        //}
        //ROUTE;
    };

}
}
