

#include "openssl/ssl.h"
#define API __declspec(dllexport)

API SSL_CTX* AKNet_SSL_CTX_new();
API int AKNet_SSL_provide_quic_data(SSL* ssl, enum ssl_encryption_level_t level, const uint8_t* data, size_t len);
API int AKNet_SSL_CTX_set_min_proto_version(SSL_CTX* ctx, uint16_t version);
API int AKNet_SSL_CTX_set_max_proto_version(SSL_CTX* ctx, uint16_t version);
API int AKNet_SSL_CTX_set_ciphersuites(SSL_CTX* ctx, const char* str);
API int AKNet_SSL_CTX_set_default_verify_paths(SSL_CTX* ctx);
API int AKNet_SSL_CTX_set_quic_method(SSL_CTX* ctx, SSL_QUIC_METHOD* meths);
API void* AKNet_SSL_get_app_data(const SSL* ssl);
API SSL_CIPHER* AKNet_SSL_get_current_cipher(const SSL* ssl);
API uint32_t AKNet_SSL_CIPHER_get_id(const SSL_CIPHER* cipher);
API long AKNet_SSL_CTX_set_session_cache_mode(SSL_CTX* ctx, long m);
API void AKNet_SSL_CTX_sess_set_new_cb(SSL_CTX* ctx, int (*new_session_cb) (struct ssl_st* ssl, SSL_SESSION* sess));
API BIO* AKNet_BIO_new();
API int AKNet_PEM_write_bio_SSL_SESSION(BIO* bio, SSL_SESSION* x);
API long AKNet_BIO_get_mem_data(BIO* bio, void* Data);
API BIO* AKNet_BIO_new_mem_buf(const void* buf, int len);
API int AKNet_BIO_free(BIO* bio);
API SSL* AKNet_SSL_new(SSL_CTX* ctx);
API int AKNet_PEM_write_bio_SSL_SESSION(BIO* bio, SSL_SESSION* x);
API SSL_SESSION* AKNet_PEM_read_bio_SSL_SESSION(BIO* bio, SSL_SESSION** x, pem_password_cb* cb, void* u);
API int AKNet_SSL_set_session(SSL* to, SSL_SESSION* session);
API void AKNet_SSL_SESSION_free(SSL_SESSION* session);
API void AKNet_SSL_set_quic_use_legacy_codepoint(SSL* ssl, int use_legacy);
API int AKNet_SSL_set_quic_transport_params(SSL* ssl, const uint8_t* params, size_t params_len);
API int AKNet_SSL_set_app_data(SSL* ssl, void* AppData);
API int AKNet_SSL_set_accept_state(SSL* ssl);
API void AKNet_SSL_set_connect_state(SSL* ssl);
API long AKNet_SSL_set_tlsext_host_name(SSL* ssl, char* url);
API int AKNet_SSL_set_alpn_protos(SSL* ssl, const unsigned char* protos, unsigned int protos_len);
API void AKNet_SSL_set_quic_early_data_enabled(SSL* ssl, int enabled);
