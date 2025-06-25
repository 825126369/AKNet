#include "boringssl_wrapper.h"

SSL_CTX* AKNet_SSL_CTX_new()
{
	return SSL_CTX_new(TLS_method());
}

int AKNet_SSL_CTX_set_min_proto_version(SSL_CTX* ctx, uint16_t version)
{
	return SSL_CTX_set_min_proto_version(ctx, version);
}

int AKNet_SSL_CTX_set_max_proto_version(SSL_CTX* ctx, uint16_t version)
{
	return SSL_CTX_set_max_proto_version(ctx, version);
}

int AKNet_SSL_CTX_set_ciphersuites(SSL_CTX* ctx, const char* str)
{
	return SSL_CTX_set_ciphersuites(ctx, str);
}

int AKNet_SSL_CTX_set_default_verify_paths(SSL_CTX* ctx)
{
	return SSL_CTX_set_default_verify_paths(ctx);
}

int AKNet_SSL_CTX_set_quic_method(SSL_CTX* ctx, SSL_QUIC_METHOD* meths)
{
	return SSL_CTX_set_quic_method(ctx, meths);
}

int AKNet_SSL_provide_quic_data(SSL* ssl, enum ssl_encryption_level_t level, const uint8_t* data, size_t len)
{
	return SSL_provide_quic_data(ssl, level, data, len);
}

void* AKNet_SSL_get_app_data(const SSL* ssl)
{
	return SSL_get_app_data(ssl);
}

const SSL_CIPHER* AKNet_SSL_get_current_cipher(const SSL* ssl)
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
	return PEM_read_bio_SSL_SESSION(bio, session, cb, u);
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

void AKNet_SSL_get_peer_quic_transport_params(SSL* ssl, const uint8_t** params, size_t* params_len)
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

unsigned long AKNet_ERR_get_error()
{
	return ERR_get_error();
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

void AKNet_SSL_set_accept_state(SSL* ssl)
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
	SSL_set_quic_early_data_enabled(ssl, enabled);
}


//-------------------------------------÷§ È------------------------------------------
int AKNet_SSL_CTX_set_max_early_data(SSL_CTX* ctx, uint32_t max_early_data)
{
	return SSL_CTX_set_max_early_data(ctx, max_early_data);
}

int AKNet_SSL_CTX_set_session_ticket_cb(SSL_CTX* ctx, SSL_CTX_generate_session_ticket_fn gen_cb,
	SSL_CTX_decrypt_session_ticket_fn dec_cb, void* arg)
{
	return SSL_CTX_set_session_ticket_cb(ctx, gen_cb, dec_cb, arg);
}

int AKNet_SSL_CTX_set_num_tickets(SSL_CTX* ctx, size_t num_tickets)
{
	return SSL_CTX_set_num_tickets(ctx, num_tickets);
}

void AKNet_SSL_CTX_set_default_passwd_cb_userdata(SSL_CTX* ctx, void* u)
{
	SSL_CTX_set_default_passwd_cb_userdata(ctx, u);
}

int AKNet_SSL_CTX_use_PrivateKey_file(SSL_CTX* ctx, const char* file, int type)
{
	return SSL_CTX_use_PrivateKey_file(ctx, file, type);
}

int AKNet_SSL_CTX_use_certificate_chain_file(SSL_CTX* ctx, const char* file)
{
	return SSL_CTX_use_certificate_chain_file(ctx, file);
}

int AKNet_SSL_CTX_use_PrivateKey(SSL_CTX* ctx, EVP_PKEY* pkey)
{
	return SSL_CTX_use_PrivateKey(ctx, pkey);
}

int AKNet_SSL_CTX_use_certificate(SSL_CTX* ctx, X509* x)
{
	return SSL_CTX_use_certificate(ctx, x);
}

long AKNet_BIO_set_mem_eof_return(BIO* bp, long larg)
{
	return BIO_set_mem_eof_return(bp, larg);
}

int AKNet_BIO_write(BIO* b, const void* data, int dlen)
{
	return BIO_write(b, data, dlen);
}

PKCS12* AKNet_d2i_PKCS12_bio(BIO* bp, PKCS12** p12)
{
	return d2i_PKCS12_bio(bp, p12);
}

int AKNet_PKCS12_parse(PKCS12* p12, const char* pass, EVP_PKEY** pkey, X509** cert, STACK_OF(X509)** ca)
{
	return PKCS12_parse(p12, pass, pkey, cert, ca);
}

void AKNet_PKCS12_free(PKCS12* p12)
{
	PKCS12_free(p12);
}

X509* AKNet_sk_X509_pop(struct stack_st_X509* st)
{
	return sk_X509_pop(st);
}

long AKNet_SSL_CTX_add_extra_chain_cert(SSL_CTX* ctx, void* parg)
{
	return SSL_CTX_add_extra_chain_cert(ctx, parg);
}

void AKNet_sk_X509_free(struct stack_st_X509* sk)
{
	sk_X509_free(sk);
}

int AKNet_SSL_CTX_check_private_key(SSL_CTX* ctx)
{
	return SSL_CTX_check_private_key(ctx);
}

int AKNet_SSL_CTX_load_verify_locations(SSL_CTX* ctx, const char* CAfile, const char* CApath)
{
	return SSL_CTX_load_verify_locations(ctx, CAfile, CApath);
}

void AKNet_SSL_CTX_set_cert_verify_callback(SSL_CTX* ctx, int (*cb) (X509_STORE_CTX*, void*), void* arg)
{
	SSL_CTX_set_cert_verify_callback(ctx, cb, arg);
}

void AKNet_SSL_CTX_set_verify(SSL_CTX* ctx, int mode, SSL_verify_cb callback)
{
	SSL_CTX_set_verify(ctx, mode, callback);
}

void AKNet_SSL_CTX_set_verify_depth(SSL_CTX* ctx, int depth)
{
	SSL_CTX_set_verify_depth(ctx, depth);
}

uint64_t AKNet_SSL_CTX_set_options(SSL_CTX* ctx, uint64_t op)
{
	return SSL_CTX_set_options(ctx, op);
}

uint64_t AKNet_SSL_CTX_clear_options(SSL_CTX* ctx, uint64_t op)
{
	return SSL_CTX_clear_options(ctx, op);
}

long AKNet_SSL_CTX_set_mode(SSL_CTX* ctx, long op)
{
	return SSL_CTX_set_mode(ctx, op);
}

void AKNet_SSL_CTX_set_alpn_select_cb(SSL_CTX* ctx, SSL_CTX_alpn_select_cb_func cb, void* arg)
{
	SSL_CTX_set_alpn_select_cb(ctx, cb, arg);
}

void AKNet_SSL_CTX_set_client_hello_cb(SSL_CTX* ctx, SSL_client_hello_cb_fn cb, void* arg)
{
	SSL_CTX_set_client_hello_cb(ctx, cb, arg);
}

void AKNet_X509_free(X509* x)
{
	X509_free(x);
}

void AKNet_EVP_PKEY_free(EVP_PKEY* pkey)
{
	EVP_PKEY_free(pkey);
}
