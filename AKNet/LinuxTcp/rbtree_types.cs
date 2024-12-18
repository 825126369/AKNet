namespace AKNet.LinuxTcp
{
    public class rb_node
    {
        public const int RB_RED = 0;
        public const int RB_BLACK = 1;

        public rb_node __rb_parent;
        public int __color;
        public rb_node rb_right;
        public rb_node rb_left;
    }

    public class rb_root
    {
        public rb_node rb_node;
    }

}
