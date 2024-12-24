namespace AKNet.LinuxTcp
{ 
    public class minmax_sample
    {
        public uint t;  /* time measurement was taken */
        public uint v;	/* value measured */
    }

    public class minmax
    {
        public minmax_sample[] s = new minmax_sample[3];
    }

    internal static partial class LinuxTcpFunc
    {
        static uint minmax_get(minmax m)
        {
            return m.s[0].v;
        }

        static uint minmax_reset(minmax m, uint t, uint meas)
        {
            minmax_sample val = new minmax_sample { t = t, v = meas };

            m.s[2] = m.s[1] = m.s[0] = val;
            return m.s[0].v;
        }

        static uint minmax_running_max(minmax m, uint win, uint t, uint meas)
        {

        }
        static uint minmax_running_min(minmax m, uint win, uint t, uint meas)
        {

        }
    }
}