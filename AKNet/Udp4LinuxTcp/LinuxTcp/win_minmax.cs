/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    public struct minmax_sample
    {
        public long t;
        public long v;
    }

    public class minmax
    {
        public readonly minmax_sample[] s = new minmax_sample[3];
    }

    internal static partial class LinuxTcpFunc
    {
        static long minmax_get(minmax m)
        {
            return m.s[0].v;
        }

        //什么时间测量的
        static long minmax_reset(minmax m, long t, long meas)
        {
            minmax_sample val = new minmax_sample { t = t, v = meas };
            m.s[2] = m.s[1] = m.s[0] = val;
            return m.s[0].v;
        }

        static long minmax_running_max(minmax m, long win, long t, long meas)
        {
            minmax_sample val = new minmax_sample { t = t, v = meas };
            if (val.v >= m.s[0].v || val.t - m.s[2].t > win)
            {
                return minmax_reset(m, t, meas);
            }

            if (val.v >= m.s[1].v)
            {
                m.s[2] = m.s[1] = val;
            }
            else if (val.v >= m.s[2].v)
            {
                m.s[2] = val;
            }
            return minmax_subwin_update(m, win, val);
        }

        static long minmax_running_min(minmax m, long win, long t, long meas)
        {
            minmax_sample val = new minmax_sample { t = t, v = meas };
            if (val.v <= m.s[0].v || val.t - m.s[2].t > win)
            {
                return minmax_reset(m, t, meas);
            }

            if (val.v <= m.s[1].v)
            {
                m.s[2] = m.s[1] = val;
            }
            else if (val.v <= m.s[2].v)
            {
                m.s[2] = val;
            }
            return minmax_subwin_update(m, win, val);
        }

        static long minmax_subwin_update(minmax m, long win, minmax_sample val)
        {
            long dt = val.t - m.s[0].t;
            if (dt > win)
            {
                m.s[0] = m.s[1];
                m.s[1] = m.s[2];
                m.s[2] = val;
                if (val.t - m.s[0].t > win)
                {
                    m.s[0] = m.s[1];
                    m.s[1] = m.s[2];
                    m.s[2] = val;
                }
            }
            else if (m.s[1].t == m.s[0].t && dt > win / 4)
            {
                m.s[2] = m.s[1] = val;
            }
            else if (m.s[2].t == m.s[1].t && dt > win / 2)
            {
                m.s[2] = val;
            }
            return m.s[0].v;
        }
    }
}