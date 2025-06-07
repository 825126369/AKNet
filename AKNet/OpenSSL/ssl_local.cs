using System;

namespace AKNet.OpenSSL
{
    public struct ssl_cipher_st
    {
        public uint valid;
        public string name;           /* text name */
        public string stdname;        /* RFC name */
        public uint id;                /* id, 4 bytes, first is version */
        public uint algorithm_mkey;    /* key exchange algorithm */
        public uint algorithm_auth;    /* server authentication */
        public uint algorithm_enc;     /* symmetric encryption */
        public uint algorithm_mac;     /* symmetric authentication */
        int min_tls;                /* minimum SSL/TLS protocol version */
        int max_tls;                /* maximum SSL/TLS protocol version */
        int min_dtls;               /* minimum DTLS protocol version */
        int max_dtls;               /* maximum DTLS protocol version */
        public uint algo_strength;     /* strength and export flags */
        public uint algorithm2;        /* Extra flags */
        public int strength_bits;      /* Number of bits really used */
        public uint alg_bits;          /* Number of bits for algorithm */
    }

    public struct ssl_session_st
    {
        public int ssl_version;
        public int master_key_length;
        public byte[] early_secret = new byte[EVP_MAX_MD_SIZE];
        public byte[] master_key = new byte[TLS13_MAX_RESUMPTION_PSK_LENGTH];
        public int session_id_length;
        public byte[] session_id = new byte[SSL_MAX_SSL_SESSION_ID_LENGTH];
        public int sid_ctx_length;
        public byte[] sid_ctx = new byte[SSL_MAX_SID_CTX_LENGTH];
        public string psk_identity_hint;
        public string psk_identity;
        int not_resumable;
        /* Peer raw public key, if available */
        EVP_PKEY* peer_rpk;
        /* This is the cert and type for the other end. */
        X509* peer;
        /* Certificate chain peer sent. */
        STACK_OF(X509) *peer_chain;
    /*
     * when app_verify_callback accepts a session where the peer's
     * certificate is not ok, we must remember the error for session reuse:
     */
    long verify_result;         /* only for servers */
        OSSL_TIME timeout;
        OSSL_TIME time;
        OSSL_TIME calc_timeout;
        unsigned int compress_meth; /* Need to lookup the method */
        const SSL_CIPHER* cipher;
        unsigned long cipher_id;    /* when ASN.1 loaded, this needs to be used to
                                 * load the 'cipher' structure */
        unsigned int kex_group;      /* TLS group from key exchange */
        CRYPTO_EX_DATA ex_data;     /* application specific data */

        struct {
            char* hostname;
            /* RFC4507 info */
            unsigned char* tick; /* Session ticket */
            size_t ticklen;      /* Session ticket length */
            /* Session lifetime hint in seconds */
            unsigned long tick_lifetime_hint;
            uint32_t tick_age_add;
            /* Max number of bytes that can be sent as early data */
            uint32_t max_early_data;
            /* The ALPN protocol selected for this session */
            unsigned char* alpn_selected;
            size_t alpn_selected_len;
            /*
             * Maximum Fragment Length as per RFC 4366.
             * If this value does not contain RFC 4366 allowed values (1-4) then
             * either the Maximum Fragment Length Negotiation failed or was not
             * performed at all.
             */
            uint8_t max_fragment_len_mode;
        }
        ext;
# ifndef OPENSSL_NO_SRP
    char* srp_username;
#endif
        unsigned char* ticket_appdata;
        size_t ticket_appdata_len;
        uint32_t flags;
        SSL_CTX* owner;

        /*
         * These are used to make removal of session-ids more efficient and to
         * implement a maximum cache size. Access requires protection of ctx->lock.
         */
        struct ssl_session_st *prev, *next;
    CRYPTO_REF_COUNT references;
    };

    struct ssl_ctx_st
    {
        // public OSSL_LIB_CTX* libctx;

        // const SSL_METHOD* method;
        public ssl_cipher_st cipher_list;

        public ssl_cipher_st cipher_list_by_id;

        public ssl_cipher_st tls13_ciphersuites;
    LHASH_OF(SSL_SESSION) *sessions;
    public int session_cache_size;
    public ssl_session_st* session_cache_head;
    public ssl_session_st *session_cache_tail;

    uint session_cache_mode;
        public long session_timeout;
        int (* new_session_cb) (struct ssl_st *ssl, SSL_SESSION* sess);
        void (* remove_session_cb) (struct ssl_ctx_st *ctx, SSL_SESSION* sess);
        SSL_SESSION* (* get_session_cb) (struct ssl_st *ssl, const unsigned char* data, int len, int* copy);

        public struct stats_DATA
        {
            TSAN_QUALIFIER int sess_connect;       /* SSL new conn - started */
            TSAN_QUALIFIER int sess_connect_renegotiate; /* SSL reneg - requested */
            TSAN_QUALIFIER int sess_connect_good;  /* SSL new conne/reneg - finished */
            TSAN_QUALIFIER int sess_accept;        /* SSL new accept - started */
            TSAN_QUALIFIER int sess_accept_renegotiate; /* SSL reneg - requested */
            TSAN_QUALIFIER int sess_accept_good;   /* SSL accept/reneg - finished */
            TSAN_QUALIFIER int sess_miss;          /* session lookup misses */
            TSAN_QUALIFIER int sess_timeout;       /* reuse attempt on timeouted session */
            TSAN_QUALIFIER int sess_cache_full;    /* session removed due to full cache */
            TSAN_QUALIFIER int sess_hit;           /* session reuse actually done */
            TSAN_QUALIFIER int sess_cb_hit;        /* session-id that was not in
                                                * the cache was passed back via
                                                * the callback. This indicates
                                                * that the application is
                                                * supplying session-id's from
                                                * other processes - spooky
                                                * :-) */
        }
        public stats_DATA stats;

#ifdef TSAN_REQUIRES_LOCKING
    CRYPTO_RWLOCK* tsan_lock;
#endif

        CRYPTO_REF_COUNT references;

        /* if defined, these override the X509_verify_cert() calls */
        int (* app_verify_callback) (X509_STORE_CTX*, void*);
    void* app_verify_arg;
        /*
         * before OpenSSL 0.9.7, 'app_verify_arg' was ignored
         * ('app_verify_callback' was called with just one argument)
         */

        /* Default password callback. */
        pem_password_cb* default_passwd_callback;

        /* Default password callback user data. */
        void* default_passwd_callback_userdata;

        /* get client cert callback */
        int (* client_cert_cb) (SSL* ssl, X509** x509, EVP_PKEY** pkey);

    /* cookie generate callback */
    int (* app_gen_cookie_cb) (SSL* ssl, unsigned char* cookie,
                              unsigned int* cookie_len);

        /* verify cookie callback */
        int (* app_verify_cookie_cb) (SSL* ssl, const unsigned char* cookie,
                                     unsigned int cookie_len);

        /* TLS1.3 app-controlled cookie generate callback */
        int (* gen_stateless_cookie_cb) (SSL* ssl, unsigned char* cookie,
                                        size_t *cookie_len);

    /* TLS1.3 verify app-controlled cookie callback */
    int (* verify_stateless_cookie_cb) (SSL* ssl, const unsigned char* cookie,
                                       size_t cookie_len);

    CRYPTO_EX_DATA ex_data;

        const EVP_MD* md5;          /* For SSLv3/TLSv1 'ssl3-md5' */
        const EVP_MD* sha1;         /* For SSLv3/TLSv1 'ssl3-sha1' */

        STACK_OF(X509) *extra_certs;
    STACK_OF(SSL_COMP) *comp_methods; /* stack of SSL_COMP, SSLv3/TLSv1 */

    /* Default values used when no per-SSL value is defined follow */

    /* used if SSL's info_callback is NULL */
    void (* info_callback) (const SSL* ssl, int type, int val);

        /*
         * What we put in certificate_authorities extension for TLS 1.3
         * (ClientHello and CertificateRequest) or just client cert requests for
         * earlier versions. If client_ca_names is populated then it is only used
         * for client cert requests, and in preference to ca_names.
         */
        STACK_OF(X509_NAME) *ca_names;
    STACK_OF(X509_NAME) *client_ca_names;

    /*
     * Default values to use in SSL structures follow (these are copied by
     * SSL_new)
     */

    uint64_t options;
        uint32_t mode;
        int min_proto_version;
        int max_proto_version;
        size_t max_cert_list;

        struct cert_st /* CERT */ *cert;
    SSL_CERT_LOOKUP* ssl_cert_info;
        int read_ahead;

        /* callback that allows applications to peek at protocol messages */
        ossl_msg_cb msg_callback;
        void* msg_callback_arg;

        uint32_t verify_mode;
        size_t sid_ctx_length;
        unsigned char sid_ctx[SSL_MAX_SID_CTX_LENGTH];
        /* called 'verify_callback' in the SSL */
        int (* default_verify_callback) (int ok, X509_STORE_CTX* ctx);

    /* Default generate session ID callback. */
    GEN_SESSION_CB generate_session_id;

        X509_VERIFY_PARAM* param;

        int quiet_shutdown;

# ifndef OPENSSL_NO_CT
        CTLOG_STORE* ctlog_store;   /* CT Log Store */
        /*
         * Validates that the SCTs (Signed Certificate Timestamps) are sufficient.
         * If they are not, the connection should be aborted.
         */
        ssl_ct_validation_cb ct_validation_callback;
        void* ct_validation_callback_arg;
#endif

        /*
         * If we're using more than one pipeline how should we divide the data
         * up between the pipes?
         */
        size_t split_send_fragment;
        /*
         * Maximum amount of data to send in one fragment. actual record size can
         * be more than this due to padding and MAC overheads.
         */
        size_t max_send_fragment;

        /* Up to how many pipelines should we use? If 0 then 1 is assumed */
        size_t max_pipelines;

        /* The default read buffer length to use (0 means not set) */
        size_t default_read_buf_len;

# ifndef OPENSSL_NO_ENGINE
        /*
         * Engine to pass requests for client certs to
         */
        ENGINE* client_cert_engine;
#endif

        /* ClientHello callback.  Mostly for extensions, but not entirely. */
        SSL_client_hello_cb_fn client_hello_cb;
        void* client_hello_cb_arg;

        /* Callback to announce new pending ssl objects in the accept queue */
        SSL_new_pending_conn_cb_fn new_pending_conn_cb;
        void* new_pending_conn_arg;

        /* TLS extensions. */
        struct {
            /* TLS extensions servername callback */
            int (* servername_cb) (SSL*, int*, void*);
        void* servername_arg;
            /* RFC 4507 session ticket keys */
            unsigned char tick_key_name[TLSEXT_KEYNAME_LENGTH];
            SSL_CTX_EXT_SECURE* secure;
# ifndef OPENSSL_NO_DEPRECATED_3_0
            /* Callback to support customisation of ticket key setting */
            int (* ticket_key_cb) (SSL* ssl,
                                  unsigned char* name, unsigned char* iv,
                                  EVP_CIPHER_CTX *ectx, HMAC_CTX* hctx, int enc);
#endif
            int (* ticket_key_evp_cb) (SSL* ssl,
                                      unsigned char* name, unsigned char* iv,
                                      EVP_CIPHER_CTX *ectx, EVP_MAC_CTX* hctx,
                                      int enc);

            /* certificate status request info */
            /* Callback for status request */
            int (* status_cb) (SSL* ssl, void* arg);
        void* status_arg;
            /* ext status type used for CSR extension (OCSP Stapling) */
            int status_type;
            /* RFC 4366 Maximum Fragment Length Negotiation */
            uint8_t max_fragment_len_mode;

            /* EC extension values inherited by SSL structure */
            size_t ecpointformats_len;
            unsigned char* ecpointformats;

            size_t supportedgroups_len;
            uint16_t* supportedgroups;

            size_t keyshares_len;
            uint16_t* keyshares;

            size_t tuples_len; /* Number of group tuples */
            size_t* tuples; /* Number of groups in each group tuple */

            /*
             * ALPN information (we are in the process of transitioning from NPN to
             * ALPN.)
             */

            /*-
             * For a server, this contains a callback function that allows the
             * server to select the protocol for the connection.
             *   out: on successful return, this must point to the raw protocol
             *        name (without the length prefix).
             *   outlen: on successful return, this contains the length of |*out|.
             *   in: points to the client's list of supported protocols in
             *       wire-format.
             *   inlen: the length of |in|.
             */
            int (* alpn_select_cb) (SSL* s,
    
                                   const unsigned char**out,
                               unsigned char* outlen,
                                   const unsigned char*in,
                               unsigned int inlen, void* arg);
            void* alpn_select_cb_arg;

            /*
             * For a client, this contains the list of supported protocols in wire
             * format.
             */
            unsigned char* alpn;
            size_t alpn_len;

# ifndef OPENSSL_NO_NEXTPROTONEG
            /* Next protocol negotiation information */

            /*
             * For a server, this contains a callback function by which the set of
             * advertised protocols can be provided.
             */
            SSL_CTX_npn_advertised_cb_func npn_advertised_cb;
            void* npn_advertised_cb_arg;
            /*
             * For a client, this contains a callback function that selects the next
             * protocol from the list provided by the server.
             */
            SSL_CTX_npn_select_cb_func npn_select_cb;
            void* npn_select_cb_arg;
#endif

            unsigned char cookie_hmac_key[SHA256_DIGEST_LENGTH];
        }
        ext;

# ifndef OPENSSL_NO_PSK
    SSL_psk_client_cb_func psk_client_callback;
        SSL_psk_server_cb_func psk_server_callback;
#endif
        SSL_psk_find_session_cb_func psk_find_session_cb;
        SSL_psk_use_session_cb_func psk_use_session_cb;

# ifndef OPENSSL_NO_SRP
        SRP_CTX srp_ctx;            /* ctx for SRP authentication */
#endif

        /* Shared DANE context */
        struct dane_ctx_st dane;

# ifndef OPENSSL_NO_SRTP
    /* SRTP profiles we are willing to do from RFC 5764 */
    STACK_OF(SRTP_PROTECTION_PROFILE) *srtp_profiles;
# endif
    /*
     * Callback for disabling session caching and ticket support on a session
     * basis, depending on the chosen cipher.
     */
    int (* not_resumable_session_cb) (SSL* ssl, int is_forward_secure);

    CRYPTO_RWLOCK*lock;

    /*
     * Callback for logging key material for use with debugging tools like
     * Wireshark. The callback should log `line` followed by a newline.
     */
    SSL_CTX_keylog_cb_func keylog_callback;

        /*
         * Private flag for internal key logging based on SSLKEYLOG env
         */
# ifndef OPENSSL_NO_SSLKEYLOG
        uint32_t do_sslkeylog;
#endif

        /*
         * The maximum number of bytes advertised in session tickets that can be
         * sent as early data.
         */
        uint32_t max_early_data;

        /*
         * The maximum number of bytes of early data that a server will tolerate
         * (which should be at least as much as max_early_data).
         */
        uint32_t recv_max_early_data;

        /* TLS1.3 padding callback */
        size_t(*record_padding_cb)(SSL* s, int type, size_t len, void* arg);
    void* record_padding_arg;
        size_t block_padding;
        size_t hs_padding;

        /* Session ticket appdata */
        SSL_CTX_generate_session_ticket_fn generate_ticket_cb;
        SSL_CTX_decrypt_session_ticket_fn decrypt_ticket_cb;
        void* ticket_cb_data;

        /* The number of TLS1.3 tickets to automatically send */
        size_t num_tickets;

        /* Callback to determine if early_data is acceptable or not */
        SSL_allow_early_data_cb_fn allow_early_data_cb;
        void* allow_early_data_cb_data;

        /* Do we advertise Post-handshake auth support? */
        int pha_enabled;

        /* Callback for SSL async handling */
        SSL_async_callback_fn async_cb;
        void* async_cb_arg;

        char* propq;

        int ssl_mac_pkey_id[SSL_MD_NUM_IDX];
        const EVP_CIPHER* ssl_cipher_methods[SSL_ENC_NUM_IDX];
        const EVP_MD* ssl_digest_methods[SSL_MD_NUM_IDX];
        size_t ssl_mac_secret_size[SSL_MD_NUM_IDX];

        size_t sigalg_lookup_cache_len;
        size_t tls12_sigalgs_len;
        /* Cache of all sigalgs we know and whether they are available or not */
        struct sigalg_lookup_st *sigalg_lookup_cache;
    /* List of all sigalgs (code points) available, incl. from providers */
    uint16_t* tls12_sigalgs;

        TLS_GROUP_INFO* group_list;
        size_t group_list_len;
        size_t group_list_max_len;

        TLS_SIGALG_INFO* sigalg_list;
        size_t sigalg_list_len;
        size_t sigalg_list_max_len;

        /* masks of disabled algorithms */
        uint32_t disabled_enc_mask;
        uint32_t disabled_mac_mask;
        uint32_t disabled_mkey_mask;
        uint32_t disabled_auth_mask;

# ifndef OPENSSL_NO_COMP_ALG
        /* certificate compression preferences */
        int cert_comp_prefs[TLSEXT_comp_cert_limit];
#endif

        /* Certificate Type stuff - for RPK vs X.509 */
        unsigned char* client_cert_type;
        size_t client_cert_type_len;
        unsigned char* server_cert_type;
        size_t server_cert_type_len;

# ifndef OPENSSL_NO_QUIC
        uint64_t domain_flags;
        SSL_TOKEN_STORE* tokencache;
#endif

# ifndef OPENSSL_NO_QLOG
        char* qlog_title; /* Session title for qlog */
#endif
    };

    struct ssl_st
    {
        public int type;
        public SSL_CTX* ctx;
        const SSL_METHOD* defltmeth;
        const SSL_METHOD* method;
        public int references;
        CRYPTO_RWLOCK*lock;
        /* extra application data */
        CRYPTO_EX_DATA ex_data;
    };

    internal static partial class OpenSSLFunc
    {

    }
}
