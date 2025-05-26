using AKNet.Common;
using AKNet.MSQuicWrapper;

namespace AKNet.MSQuic.Binding
{
    internal unsafe class QuicConnection
    {
        public void Init()
        {
            bool Status = true;
            QUIC_API_TABLE* MsQuic = null;
            if(MSQuicWrapperFunc.MsQuicOpen2(&MsQuic) != 0)
            {
                NetLogMgr.LogError("Failed to open MsQuic API.");
                return;
            }

            if (MsQuic->RegistrationOpen(RegConfig, Registration) != 0)
            {
                NetLogMgr.LogError(string.Format("RegistrationOpen failed, 0x%x!\n", Status));
                return;
            }
        }
    }
}
