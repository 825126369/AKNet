

#include <openssl/ssl.h>
#define API __declspec(dllexport)


API SSL_CTX* AKNet_SSL_CTX_new();
API int AKNet_SSL_provide_quic_data(SSL* ssl, enum ssl_encryption_level_t level, const uint8_t* data, size_t len);
