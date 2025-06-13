#include "boringssl_wrapper.h"

int AKNet_SSL_provide_quic_data(SSL* ssl, enum ssl_encryption_level_t level, const uint8_t* data, size_t len)
{
	return SSL_provide_quic_data(ssl, level, data, len);
}

SSL_CTX* AKNet_SSL_CTX_new()
{
	return SSL_CTX_new(TLS_method());
}

int AKNet_SSL_CTX_set_min_proto_version(SSL_CTX* ctx, uint16_t version)
{
	SSL_CTX_set_min_proto_version(ctx, version);
}

int AKNet_SSL_CTX_set_max_proto_version(SSL_CTX* ctx, uint16_t version)
{
	SSL_CTX_set_max_proto_version(ctx, version);
}

int AKNet_SSL_CTX_set_ciphersuites(SSL_CTX* ctx, const char* str)
{
	return SSL_CTX_set_cipher_list(ctx, str);
}

int AKNet_SSL_CTX_set_default_verify_paths(SSL_CTX* ctx)
{
	return SSL_CTX_set_default_verify_paths(ctx);
}

int AKNet_SSL_CTX_set_quic_method(SSL_CTX* ctx, SSL_QUIC_METHOD* meths)
{
	return SSL_CTX_set_quic_method(ctx, meths);
}

void* AKNet_SSL_get_app_data(const SSL* ssl)
{
	return SSL_get_app_data(ssl);
}

SSL_CIPHER* AKNet_SSL_get_current_cipher(const SSL* ssl)
{
	return SSL_get_current_cipher(ssl);
}

uint32_t AKNet_SSL_CIPHER_get_id(const SSL_CIPHER* cipher)
{
	return SSL_CIPHER_get_id(cipher);
}
