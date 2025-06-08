//namespace AKNet.OpenSSL
//{
//    public class x509_st
//    {
//        X509_CINF cert_info;
//        X509_ALGOR sig_alg;
//        ASN1_BIT_STRING signature;
//        X509_SIG_INFO siginf;
//        CRYPTO_REF_COUNT references;
//        CRYPTO_EX_DATA ex_data;
//        long ex_pathlen;
//        long ex_pcpathlen;
//        uint32_t ex_flags;
//        uint32_t ex_kusage;
//        uint32_t ex_xkusage;
//        uint32_t ex_nscert;
//        ASN1_OCTET_STRING* skid;
//        AUTHORITY_KEYID* akid;
//        X509_POLICY_CACHE* policy_cache;
//        STACK_OF(DIST_POINT) *crldp;
//        STACK_OF(GENERAL_NAME) *altname;
//        NAME_CONSTRAINTS* nc;
//    # ifndef OPENSSL_NO_RFC3779
//            STACK_OF(IPAddressFamily) *rfc3779_addr;
//        struct ASIdentifiers_st *rfc3779_asid;
//    # endif
//        unsigned char sha1_hash[SHA_DIGEST_LENGTH];
//        X509_CERT_AUX* aux;
//        CRYPTO_RWLOCK*lock;
//        volatile int ex_cached;
//        /* Set on live certificates for authentication purposes */
//        ASN1_OCTET_STRING* distinguishing_id;
//        OSSL_LIB_CTX* libctx;
//        char* propq;
//    }

//    public class X509_algor_st
//    {
//        ASN1_OBJECT* algorithm;
//        ASN1_TYPE* parameter;
//    }

//    internal class x509_cinf_st
//    {
//        public asn1_string_st version;      /* [ 0 ] default of v1 */
//        public asn1_string_st serialNumber;
//        X509_ALGOR signature;
//        X509_NAME* issuer;
//        X509_VAL validity;
//        X509_NAME* subject;
//        X509_PUBKEY* key;
//        ASN1_BIT_STRING* issuerUID; /* [ 1 ] optional in v2 */
//        ASN1_BIT_STRING* subjectUID; /* [ 2 ] optional in v2 */
//        STACK_OF(X509_EXTENSION) *extensions; /* [ 3 ] optional in v3 */
//        ASN1_ENCODING enc;
//    }

//}
