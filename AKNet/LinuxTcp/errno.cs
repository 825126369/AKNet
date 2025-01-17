/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    public static class ErrorCode
    {
        public const int EPERM = 1; /* Operation not permitted */
        public const int ENOENT = 2;    /* No such file or directory */
        public const int ESRCH = 3; /* No such process */
        public const int EINTR = 4; /* Interrupted system call */
        public const int EIO = 5;   /* I/O error */
        public const int ENXIO = 6; /* No such device or address */
        public const int E2BIG = 7; /* Argument list too long */
        public const int ENOEXEC = 8;   /* Exec format error */
        public const int EBADF = 9; /* Bad file number */
        public const int ECHILD = 10;   /* No child processes */
        public const int EAGAIN = 11;   /* Try again */
        public const int ENOMEM = 12;   /* Out of memory */
        public const int EACCES = 13;   /* Permission denied */
        public const int EFAULT = 14;   /* Bad address */
        public const int ENOTBLK = 15;  /* Block device required */
        public const int EBUSY = 16;    /* Device or resource busy */
        public const int EEXIST = 17;   /* File exists */
        public const int EXDEV = 18;    /* Cross-device link */
        public const int ENODEV = 19;   /* No such device */
        public const int ENOTDIR = 20;  /* Not a directory */
        public const int EISDIR = 21;   /* Is a directory */
        public const int EINVAL = 22;   /* Invalid argument */
        public const int ENFILE = 23;   /* File table overflow */
        public const int EMFILE = 24;   /* Too many open files */
        public const int ENOTTY = 25;   /* Not a typewriter */
        public const int ETXTBSY = 26;  /* Text file busy */
        public const int EFBIG = 27;    /* File too large */
        public const int ENOSPC = 28;   /* No space left on device */
        public const int ESPIPE = 29;   /* Illegal seek */
        public const int EROFS = 30;    /* Read-only file system */
        public const int EMLINK = 31;   /* Too many links */
        public const int EPIPE = 32;    /* Broken pipe */
        public const int EDOM = 33; /* Math argument out of domain of func */
        public const int ERANGE = 34;   /* Math result not representable */


        public const int ENOMSG = 35;   /* No message of desired type */
        public const int EIDRM = 36;    /* Identifier removed */
        public const int ECHRNG = 37;   /* Channel number out of range */
        public const int EL2NSYNC = 38; /* Level 2 not synchronized */
        public const int EL3HLT = 39;   /* Level 3 halted */
        public const int EL3RST = 40;   /* Level 3 reset */
        public const int ELNRNG = 41;   /* Link number out of range */
        public const int EUNATCH = 42;  /* Protocol driver not attached */
        public const int ENOCSI = 43;   /* No CSI structure available */
        public const int EL2HLT = 44;   /* Level 2 halted */
        public const int EDEADLK = 45;  /* Resource deadlock would occur */
        public const int ENOLCK = 46;   /* No record locks available */
        public const int EBADE = 50;    /* Invalid exchange */
        public const int EBADR = 51;    /* Invalid request descriptor */
        public const int EXFULL = 52;   /* Exchange full */
        public const int ENOANO = 53;   /* No anode */
        public const int EBADRQC = 54;  /* Invalid request code */
        public const int EBADSLT = 55;  /* Invalid slot */
        public const int EDEADLOCK = 56;    /* File locking deadlock error */
        public const int EBFONT = 59;   /* Bad font file format */
        public const int ENOSTR = 60;   /* Device not a stream */
        public const int ENODATA = 61;  /* No data available */
        public const int ETIME = 62;    /* Timer expired */
        public const int ENOSR = 63;    /* Out of streams resources */
        public const int ENONET = 64;   /* Machine is not on the network */
        public const int ENOPKG = 65;   /* Package not installed */
        public const int EREMOTE = 66;  /* Object is remote */
        public const int ENOLINK = 67;  /* Link has been severed */
        public const int EADV = 68; /* Advertise error */
        public const int ESRMNT = 69;   /* Srmount error */
        public const int ECOMM = 70;    /* Communication error on send */
        public const int EPROTO = 71;   /* Protocol error */
        public const int EDOTDOT = 73;  /* RFS specific error */
        public const int EMULTIHOP = 74;    /* Multihop attempted */
        public const int EBADMSG = 77;  /* Not a data message */
        public const int ENAMETOOLONG = 78; /* File name too long */
        public const int EOVERFLOW = 79;    /* Value too large for defined data type */
        public const int ENOTUNIQ = 80; /* Name not unique on network */
        public const int EBADFD = 81;   /* File descriptor in bad state */
        public const int EREMCHG = 82;  /* Remote address changed */
        public const int ELIBACC = 83;  /* Can not access a needed shared library */
        public const int ELIBBAD = 84;  /* Accessing a corrupted shared library */
        public const int ELIBSCN = 85;  /* .lib section in a.out corrupted */
        public const int ELIBMAX = 86;  /* Attempting to link in too many shared libraries */
        public const int ELIBEXEC = 87; /* Cannot exec a shared library directly */
        public const int EILSEQ = 88;   /* Illegal byte sequence */
        public const int ENOSYS = 89;   /* Function not implemented */
        public const int ELOOP = 90;    /* Too many symbolic links encountered */
        public const int ERESTART = 91; /* Interrupted system call should be restarted */
        public const int ESTRPIPE = 92; /* Streams pipe error */
        public const int ENOTEMPTY = 93;    /* Directory not empty */
        public const int EUSERS = 94;   /* Too many users */
        public const int ENOTSOCK = 95; /* Socket operation on non-socket */
        public const int EDESTADDRREQ = 96; /* Destination address required */
        public const int EMSGSIZE = 97; /* Message too long */
        public const int EPROTOTYPE = 98;   /* Protocol wrong type for socket */
        public const int ENOPROTOOPT = 99;  /* Protocol not available */
        public const int EPROTONOSUPPORT = 120; /* Protocol not supported */
        public const int ESOCKTNOSUPPORT = 121; /* Socket type not supported */
        public const int EOPNOTSUPP = 122;  /* Operation not supported on transport endpoint */
        public const int EPFNOSUPPORT = 123;    /* Protocol family not supported */
        public const int EAFNOSUPPORT = 124;    /* Address family not supported by protocol */
        public const int EADDRINUSE = 125;  /* Address already in use */
        public const int EADDRNOTAVAIL = 126;   /* Cannot assign requested address */
        public const int ENETDOWN = 127;    /* Network is down */
        public const int ENETUNREACH = 128; /* Network is unreachable */
        public const int ENETRESET = 129;   /* Network dropped connection because of reset */
        public const int ECONNABORTED = 130;    /* Software caused connection abort */
        public const int ECONNRESET = 131;  /* Connection reset by peer */
        public const int ENOBUFS = 132; /* No buffer space available */
        public const int EISCONN = 133; /* Transport endpoint is already connected */
        public const int ENOTCONN = 134;    /* Transport endpoint is not connected */
        public const int EUCLEAN = 135; /* Structure needs cleaning */
        public const int ENOTNAM = 137; /* Not a XENIX named type file */
        public const int ENAVAIL = 138; /* No XENIX semaphores available */
        public const int EISNAM = 139;  /* Is a named type file */
        public const int EREMOTEIO = 140;   /* Remote I/O error */
        public const int EINIT = 141;   /* Reserved */
        public const int EREMDEV = 142; /* Error 142 */
        public const int ESHUTDOWN = 143;   /* Cannot send after transport endpoint shutdown */
        public const int ETOOMANYREFS = 144;    /* Too many references: cannot splice */
        public const int ETIMEDOUT = 145;   /* Connection timed out */
        public const int ECONNREFUSED = 146;    /* Connection refused */
        public const int EHOSTDOWN = 147;   /* Host is down */
        public const int EHOSTUNREACH = 148;    /* No route to host */
        public const int EWOULDBLOCK = 11;  /* Operation would block */
        public const int EALREADY = 149;    /* Operation already in progress */
        public const int EINPROGRESS =	150;    /* Operation now in progress */
        public const int ESTALE = 151;  /* Stale file handle */
        public const int ECANCELED = 158;   /* AIO operation canceled */

        /*
         * These error are Linux extensions.
         */
        public const int ENOMEDIUM = 159;   /* No medium found */
        public const int EMEDIUMTYPE = 160; /* Wrong medium type */
        public const int ENOKEY = 161;  /* Required key not available */
        public const int EKEYEXPIRED = 162; /* Key has expired */
        public const int EKEYREVOKED = 163; /* Key has been revoked */
        public const int EKEYREJECTED = 164;    /* Key was rejected by service */

        /* for robust mutexes */
        public const int EOWNERDEAD = 165;  /* Owner died */
        public const int ENOTRECOVERABLE = 166; /* State not recoverable */

        public const int ERFKILL = 167; /* Operation not possible due to RF-kill */

        public const int EHWPOISON = 168;   /* Memory page has hardware error */

        public const int EDQUOT = 1133;	/* Quota exceeded */
    }
}
