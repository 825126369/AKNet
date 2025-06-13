

#include <openssl/ssl.h>
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

