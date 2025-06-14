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

long AKNet_SSL_CTX_set_session_cache_mode(SSL_CTX* ctx, long m)
{
	return SSL_CTX_set_session_cache_mode(ctx, m);
}

void AKNet_SSL_CTX_sess_set_new_cb(SSL_CTX* ctx, int (*new_session_cb) (struct ssl_st* ssl, SSL_SESSION* sess))
{
	 SSL_CTX_sess_set_new_cb(ctx, new_session_cb);
}

//-------------------------------------------------------------------------------
BIO* AKNet_BIO_new()
{
	return BIO_new(BIO_s_mem());
}

int AKNet_BIO_free(BIO* bio)
{
	return BIO_free(bio);
}

long AKNet_BIO_get_mem_data(BIO* bio, void* Data)
{
	return BIO_get_mem_data(bio, Data);
}

BIO* AKNet_BIO_new_mem_buf(const void* buf, int len)
{
	return BIO_new_mem_buf(buf, len);
}

//-------------------------------------------------------------------------------------------------------

int AKNet_PEM_write_bio_SSL_SESSION(BIO* bio, SSL_SESSION* session)
{
	return PEM_write_bio_SSL_SESSION(bio, session);
}

SSL_SESSION* AKNet_PEM_read_bio_SSL_SESSION(BIO* bio, SSL_SESSION** session, pem_password_cb* cb, void* u)
{
	PEM_read_bio_SSL_SESSION(bio, session, cb, u);
}

int AKNet_SSL_set_session(SSL* to, SSL_SESSION* session)
{
	return SSL_set_session(to, session);
}

SSL_SESSION* AKNet_SSL_get_session(SSL* ssl)
{
	return SSL_get_session(ssl);
}

void AKNet_SSL_SESSION_free(SSL_SESSION* session)
{
	SSL_SESSION_free(session);
}

void AKNet_SSL_set_quic_use_legacy_codepoint(SSL* ssl, int use_legacy)
{
	SSL_set_quic_use_legacy_codepoint(ssl, use_legacy);
}

int AKNet_SSL_set_quic_transport_params(SSL* ssl, const uint8_t* params, size_t params_len)
{
	return SSL_set_quic_transport_params(ssl, params, params_len);
}

int AKNet_SSL_get_peer_quic_transport_params(SSL* ssl, const uint8_t** params, size_t* params_len)
{
	SSL_get_peer_quic_transport_params(ssl, params, params_len);
}

int AKNet_SSL_SESSION_set1_ticket_appdata(SSL_SESSION* session, void* data, int nLength)
{
	return SSL_SESSION_set1_ticket_appdata(session, data, nLength);
}

int AKNet_SSL_process_quic_post_handshake(SSL* ssl)
{
	return SSL_process_quic_post_handshake(ssl);
}

int AKNet_SSL_new_session_ticket(SSL* ssl)
{
	return SSL_new_session_ticket(ssl);
}

int AKNet_SSL_do_handshake(SSL* ssl)
{
	return SSL_do_handshake(ssl);
}

int AKNet_SSL_session_reused(SSL* ssl)
{
	return SSL_session_reused(ssl);
}

int AKNet_SSL_get_early_data_status(SSL* ssl)
{
	return SSL_get_early_data_status(ssl);
}

void AKNet_SSL_get0_alpn_selected(const SSL* ssl, const unsigned char** data, unsigned int* len)
{
	SSL_get0_alpn_selected(ssl, data, len);
}

int AKNet_SSL_get_error(SSL* ssl, int ret_code)
{
	return SSL_get_error(ssl, ret_code);
}

//----------------------------------------------------------------------------------------------------

SSL* AKNet_SSL_new(SSL_CTX* ctx)
{
	return SSL_new(ctx);
}

int AKNet_SSL_set_app_data(SSL* ssl, void* AppData)
{
	return SSL_set_app_data(ssl, AppData);
}

int AKNet_SSL_set_accept_state(SSL* ssl)
{
	SSL_set_accept_state(ssl);
}

void AKNet_SSL_set_connect_state(SSL* ssl)
{
	SSL_set_connect_state(ssl);
}

long AKNet_SSL_set_tlsext_host_name(SSL* ssl, char* url)
{
	return SSL_set_tlsext_host_name(ssl, url);
}

int AKNet_SSL_set_alpn_protos(SSL* ssl, const unsigned char* protos, unsigned int protos_len)
{
	return SSL_set_alpn_protos(ssl, protos, protos_len);
}

void AKNet_SSL_set_quic_early_data_enabled(SSL* ssl, int enabled)
{
	return SSL_set_quic_early_data_enabled(ssl, enabled);
}

