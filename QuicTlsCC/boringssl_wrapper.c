#include "boringssl_wrapper.h"

int AKNet_SSL_provide_quic_data(SSL* ssl, enum ssl_encryption_level_t level, const uint8_t* data, size_t len)
{
	return SSL_provide_quic_data(ssl, level, data, len);
}

SSL_CTX* AKNet_SSL_CTX_new()
{
	return SSL_CTX_new(TLS_method());
}
