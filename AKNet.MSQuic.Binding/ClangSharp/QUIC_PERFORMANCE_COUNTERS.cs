namespace AKNet.MSQuicWrapper
{
    public enum QUIC_PERFORMANCE_COUNTERS
    {
        QUIC_PERF_COUNTER_CONN_CREATED,
        QUIC_PERF_COUNTER_CONN_HANDSHAKE_FAIL,
        QUIC_PERF_COUNTER_CONN_APP_REJECT,
        QUIC_PERF_COUNTER_CONN_RESUMED,
        QUIC_PERF_COUNTER_CONN_ACTIVE,
        QUIC_PERF_COUNTER_CONN_CONNECTED,
        QUIC_PERF_COUNTER_CONN_PROTOCOL_ERRORS,
        QUIC_PERF_COUNTER_CONN_NO_ALPN,
        QUIC_PERF_COUNTER_STRM_ACTIVE,
        QUIC_PERF_COUNTER_PKTS_SUSPECTED_LOST,
        QUIC_PERF_COUNTER_PKTS_DROPPED,
        QUIC_PERF_COUNTER_PKTS_DECRYPTION_FAIL,
        QUIC_PERF_COUNTER_UDP_RECV,
        QUIC_PERF_COUNTER_UDP_SEND,
        QUIC_PERF_COUNTER_UDP_RECV_BYTES,
        QUIC_PERF_COUNTER_UDP_SEND_BYTES,
        QUIC_PERF_COUNTER_UDP_RECV_EVENTS,
        QUIC_PERF_COUNTER_UDP_SEND_CALLS,
        QUIC_PERF_COUNTER_APP_SEND_BYTES,
        QUIC_PERF_COUNTER_APP_RECV_BYTES,
        QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH,
        QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH,
        QUIC_PERF_COUNTER_CONN_OPER_QUEUED,
        QUIC_PERF_COUNTER_CONN_OPER_COMPLETED,
        QUIC_PERF_COUNTER_WORK_OPER_QUEUE_DEPTH,
        QUIC_PERF_COUNTER_WORK_OPER_QUEUED,
        QUIC_PERF_COUNTER_WORK_OPER_COMPLETED,
        QUIC_PERF_COUNTER_PATH_VALIDATED,
        QUIC_PERF_COUNTER_PATH_FAILURE,
        QUIC_PERF_COUNTER_SEND_STATELESS_RESET,
        QUIC_PERF_COUNTER_SEND_STATELESS_RETRY,
        QUIC_PERF_COUNTER_CONN_LOAD_REJECT,
        QUIC_PERF_COUNTER_MAX,
    }
}
