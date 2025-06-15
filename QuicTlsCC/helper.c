#include "boringssl_wrapper.h"

void print_openssl_errors()
{
	printf("print_openssl_errors\n");
	//fprintf(stderr, "failed error: %d\n", ERR_get_error());
	ERR_print_errors_fp(stderr);
}
