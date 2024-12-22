namespace AKNet.LinuxTcp
{
    public class ip_mc_socklist
    {
        public ip_mc_socklist __rcu* next_rcu;
        public ip_mreqn multi;
        public uint sfmode;
        public ip_sf_socklist __rcu* sflist;
        public rcu_head rcu;
    }
}
