using System;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_STORAGE
    {
        //HKEY RegKey;
        //HANDLE NotifyEvent;
        //PTP_WAIT ThreadPoolWait;
        //CXPLAT_STORAGE_CHANGE_CALLBACK_HANDLER Callback;
        //public Action CallbackContext;
    }

    internal static partial class MSQuicFunc
    {
        long CxPlatStorageReadValue(CXPLAT_STORAGE Storage, string Name, byte[] Buffer, int BufferLength)
        {
            //DWORD Type;
            //return HRESULT_FROM_WIN32(RegQueryValueExA(Storage->RegKey, Name, NULL,&Type, Buffer,(PDWORD)BufferLength));
            return 0;
        }
    }
}
