

#include "openssl/ssl.h"
#include "openssl/err.h"
#include "openssl/pkcs12.h"

#define API __declspec(dllexport)

API SSL_CTX* AKNet_SSL_CTX_new();
API void AKNet_SSL_CTX_free(SSL_CTX* x);
API int AKNet_SSL_provide_quic_data(SSL* ssl, enum ssl_encryption_level_t level, const uint8_t* data, size_t len);
API int AKNet_SSL_CTX_set_min_proto_version(SSL_CTX* ctx, uint16_t version);
API int AKNet_SSL_CTX_set_max_proto_version(SSL_CTX* ctx, uint16_t version);
API int AKNet_SSL_CTX_set_ciphersuites(SSL_CTX* ctx, const char* str);
API int AKNet_SSL_CTX_set_default_verify_paths(SSL_CTX* ctx);
API int AKNet_SSL_CTX_set_quic_method(SSL_CTX* ctx, SSL_QUIC_METHOD* meths);
API void* AKNet_SSL_get_app_data(const SSL* ssl);
API const SSL_CIPHER* AKNet_SSL_get_current_cipher(const SSL* ssl);
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
API void AKNet_SSL_set_accept_state(SSL* ssl);
API void AKNet_SSL_set_connect_state(SSL* ssl);
API long AKNet_SSL_set_tlsext_host_name(SSL* ssl, char* url);
API int AKNet_SSL_set_alpn_protos(SSL* ssl, const unsigned char* protos, unsigned int protos_len);
API void AKNet_SSL_set_quic_early_data_enabled(SSL* ssl, int enabled);
API SSL_SESSION* AKNet_SSL_get_session(SSL* ssl);
API int AKNet_SSL_SESSION_set1_ticket_appdata(SSL_SESSION* session, void* data, int nLength);
API void AKNet_SSL_get_peer_quic_transport_params(SSL* ssl, const uint8_t** params, size_t* params_len);
API int AKNet_SSL_SESSION_set1_ticket_appdata(SSL_SESSION* session, void* data, int nLength);
API int AKNet_SSL_process_quic_post_handshake(SSL* ssl);
API int AKNet_SSL_new_session_ticket(SSL* ssl);
API int AKNet_SSL_do_handshake(SSL* ssl);
API int AKNet_SSL_session_reused(SSL* ssl);
API int AKNet_SSL_get_early_data_status(SSL* ssl);
API void AKNet_SSL_get0_alpn_selected(const SSL* ssl, const unsigned char** data, unsigned int* len);
API int AKNet_SSL_get_error(SSL* ssl, int ret_code);

API void print_openssl_errors();
//API EVP_CIPHER_CTX* AKNet_EVP_CIPHER_CTX_new();
//API int AKNet_EVP_EncryptInit_ex(EVP_CIPHER_CTX* ctx, const EVP_CIPHER* cipher, ENGINE* impl, const unsigned char* key, const unsigned char* iv);
//API int AKNet_EVP_EncryptUpdate(EVP_CIPHER_CTX* ctx, unsigned char* out, int* outl, const unsigned char* in, int inl);


API int AKNet_SSL_CTX_set_max_early_data(SSL_CTX* ctx, uint32_t max_early_data);
API int AKNet_SSL_CTX_set_session_ticket_cb(SSL_CTX* ctx, SSL_CTX_generate_session_ticket_fn gen_cb,
	SSL_CTX_decrypt_session_ticket_fn dec_cb, void* arg);
API int AKNet_SSL_CTX_set_num_tickets(SSL_CTX* ctx, size_t num_tickets);
API void AKNet_SSL_CTX_set_default_passwd_cb_userdata(SSL_CTX* ctx, void* u);
API int AKNet_SSL_CTX_use_PrivateKey_file(SSL_CTX* ctx, const char* file, int type);
API int AKNet_SSL_CTX_use_certificate_chain_file(SSL_CTX* ctx, const char* file);
API int AKNet_SSL_CTX_use_PrivateKey(SSL_CTX* ctx, EVP_PKEY* pkey);
API int AKNet_SSL_CTX_use_certificate(SSL_CTX* ctx, X509* x);
API long AKNet_BIO_set_mem_eof_return(BIO* bp, long larg);
API int AKNet_BIO_write(BIO* b, const void* data, int dlen);
API PKCS12* AKNet_d2i_PKCS12_bio(BIO* bp, PKCS12** p12);
API int AKNet_PKCS12_parse(PKCS12* p12, const char* pass, EVP_PKEY** pkey, X509** cert, STACK_OF(X509)** ca);
API void AKNet_PKCS12_free(PKCS12* p12);
API long AKNet_SSL_CTX_add_extra_chain_cert(SSL_CTX* ctx, void* parg);
API void AKNet_sk_X509_free(struct stack_st_X509* sk);
API int AKNet_SSL_CTX_check_private_key(SSL_CTX* ctx);
API int AKNet_SSL_CTX_load_verify_locations(SSL_CTX* ctx, const char* CAfile, const char* CApath);
API void AKNet_SSL_CTX_set_cert_verify_callback(SSL_CTX* ctx, int (*cb) (X509_STORE_CTX*, void*), void* arg);
API void AKNet_SSL_CTX_set_verify(SSL_CTX* ctx, int mode, SSL_verify_cb callback);
API void AKNet_SSL_CTX_set_verify_depth(SSL_CTX* ctx, int depth);
API uint64_t AKNet_SSL_CTX_set_options(SSL_CTX* ctx, uint64_t op);
API uint64_t AKNet_SSL_CTX_clear_options(SSL_CTX* ctx, uint64_t op);
API long AKNet_SSL_CTX_set_mode(SSL_CTX* ctx, long op);
API void AKNet_SSL_CTX_set_alpn_select_cb(SSL_CTX* ctx, SSL_CTX_alpn_select_cb_func cb, void* arg);
API void AKNet_SSL_CTX_set_client_hello_cb(SSL_CTX* ctx, SSL_client_hello_cb_fn cb, void* arg);
API void AKNet_X509_free(X509* x);
API void AKNet_EVP_PKEY_free(EVP_PKEY* pkey);
API int AKNet_SSL_SESSION_get0_ticket_appdata(SSL_SESSION* ss, void** data, size_t* len);
API int AKNet_SSL_client_hello_get0_ext(SSL* s, unsigned int type, const unsigned char** out, size_t* outlen);


API X509* AKNet_sk_X509_pop(struct stack_st_X509* st);
API X509* AKNet_X509_STORE_CTX_get0_cert(const X509_STORE_CTX* ctx);
API void* AKNet_X509_STORE_CTX_get_ex_data(const X509_STORE_CTX* ctx);
API int AKNet_X509_verify_cert(X509_STORE_CTX* ctx);
API void AKNet_X509_STORE_CTX_set_error(X509_STORE_CTX* ctx, int s);
API int AKNet_X509_STORE_CTX_get_error(const X509_STORE_CTX* ctx);
API void AKNet_OPENSSL_free(void* ptr);

API X509* AKNet_X509_STORE_CTX_get0_cert(const X509_STORE_CTX* ctx);
API void* AKNet_X509_STORE_CTX_get_ex_data(const X509_STORE_CTX* ctx);
API int AKNet_i2d_X509(const X509* x, unsigned char** outBuf);
API int AKNet_i2d_PKCS7(const PKCS7* x, unsigned char** outBuf);
API struct stack_st_X509* AKNet_X509_STORE_CTX_get0_chain(const X509_STORE_CTX* ctx);
API int AKNet_sk_X509_num(struct stack_st_X509* ctx);
API PKCS7* AKNet_PKCS7_new();
API void AKNet_PKCS7_free(PKCS7* a);
API int AKNet_PKCS7_set_type(PKCS7* p7, int type);
API int AKNet_PKCS7_content_new(PKCS7* p7, int nid);
API int AKNet_PKCS7_add_certificate(PKCS7* p7, X509* x);
API X509* AKNet_sk_X509_value(struct stack_st_X509* sk, int idx);

