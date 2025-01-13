using System;
using System.Xml.Linq;

namespace AKNet.LinuxTcp
{
    internal class rb_node
    {
        public sk_buff value;
        public byte color;
        public rb_node parent;
        public rb_node rb_right;
        public rb_node rb_left;
    }

    internal class rb_root
    {
        public rb_node rb_node = null;
    }

    internal static partial class LinuxTcpFunc
    {
        public const byte RB_EMPTY = 0;
        public const byte RB_RED = 1;
        public const byte RB_BLACK = 2;

        static byte rb_color(rb_node rb)
        {
            return rb.color;
        }

        static bool rb_is_red(rb_node rb)
        {
            return rb.color == RB_RED;
        }

        static bool rb_is_black(rb_node rb)
        {
            return rb.color == RB_BLACK;
        }

        static rb_root RB_ROOT()
        {
            return new rb_root();
        }

        static void rb_set_black(rb_node rb)
        {
            rb.color = RB_BLACK;
        }

        static rb_node rb_red_parent(rb_node red)
        {
            return red.parent;
        }

        static rb_node rb_parent(rb_node node)
        {
            return node.parent;
        }

        static sk_buff rb_entry(rb_node node)
        {
            return node.value;
        }

        static bool RB_EMPTY_ROOT(rb_root node)
        {
            return node.rb_node == null;
        }

        static bool RB_EMPTY_NODE(rb_node node)
        {
            return node.color == RB_EMPTY;
        }

        static void RB_CLEAR_NODE(rb_node node)
        {
            node.color = RB_EMPTY;
        }

        static void rb_set_parent(rb_node rb, rb_node p)
        {
            rb.parent = p;
        }

        static void rb_set_parent_color(rb_node rb, rb_node p, byte color)
        {
            rb.parent = p;
            rb.color = color;
        }

        static void __rb_change_child(rb_node oldNode, rb_node newNode, rb_node parent, rb_root root)
        {
            if (parent != null)
            {
                if (parent.rb_left == oldNode)
                {
                    parent.rb_left = newNode;
                }
                else
                {
                    parent.rb_right = newNode;
                }
            }
            else
            {
                root.rb_node = newNode;
            }
        }

        static void __rb_rotate_set_parents(rb_node oldNode, rb_node newNode, rb_root root, byte color)
        {
            rb_node parent = rb_parent(oldNode);
            newNode.parent = oldNode.parent;
            newNode.color = oldNode.color;

            rb_set_parent_color(oldNode, newNode, color);
            __rb_change_child(oldNode, newNode, parent, root);
        }

        static void __rb_insert(rb_node node, rb_root root, Action<rb_node, rb_node> augment_rotate)
        {
            rb_node parent = rb_red_parent(node), gparent, tmp;

            while (true)
            {
                if (parent == null)
                {
                    rb_set_parent_color(node, null, RB_BLACK);
                    break;
                }

                if (rb_is_black(parent))
                {
                    break;
                }

                gparent = rb_red_parent(parent);
                tmp = gparent.rb_right;

                if (parent != tmp)
                {
                    if (tmp != null && rb_is_red(tmp))
                    {
                        /*
				         * Case 1 - node's uncle is red (color flips).
				         *
				         *       G            g
				         *      / \          / \
				         *     p   u  -->   P   U
				         *    /            /
				         *   n            n
				         *
				         * However, since g's parent might be red, and
				         * 4) does not allow this, we need to recurse
				         * at g.
				         */
                        rb_set_parent_color(tmp, gparent, RB_BLACK);
                        rb_set_parent_color(parent, gparent, RB_BLACK);
                        node = gparent;
                        parent = rb_parent(node);
                        rb_set_parent_color(node, parent, RB_RED);
                        continue;
                    }

                    tmp = parent.rb_right;
                    if (node == tmp)
                    {
                        /*
                         * Case 2 - node's uncle is black and node is
                         * the parent's right child (left rotate at parent).
                         *
                         *      G             G
                         *     / \           / \
                         *    p   U  -->    n   U
                         *     \           /
                         *      n         p
                         *
                         * This still leaves us in violation of 4), the
                         * continuation into Case 3 will fix that.
                         */
                        tmp = node.rb_left;
                        parent.rb_right = tmp;
                        node.rb_left = parent;
                        if (tmp != null)
                        {
                            rb_set_parent_color(tmp, parent, RB_BLACK);
                        }

                        rb_set_parent_color(parent, node, RB_RED);
                        augment_rotate(parent, node);
                        parent = node;
                        tmp = node.rb_right;
                    }

                    /*
                     * Case 3 - node's uncle is black and node is
                     * the parent's left child (right rotate at gparent).
                     *
                     *        G           P
                     *       / \         / \
                     *      p   U  -->  n   g
                     *     /                 \
                     *    n                   U
                     */
                    gparent.rb_left = tmp; /* == parent->rb_right */
                    parent.rb_right = gparent;
                    if (tmp != null)
                    {
                        rb_set_parent_color(tmp, gparent, RB_BLACK);
                    }
                    __rb_rotate_set_parents(gparent, parent, root, RB_RED);
                    augment_rotate(gparent, parent);
                    break;
                }
                else
                {
                    tmp = gparent.rb_left;
                    if (tmp != null && rb_is_red(tmp))
                    {
                        /* Case 1 - color flips */
                        rb_set_parent_color(tmp, gparent, RB_BLACK);
                        rb_set_parent_color(parent, gparent, RB_BLACK);
                        node = gparent;
                        parent = rb_parent(node);
                        rb_set_parent_color(node, parent, RB_RED);
                        continue;
                    }

                    tmp = parent.rb_left;
                    if (node == tmp)
                    {
                        /* Case 2 - right rotate at parent */
                        tmp = node.rb_right;
                        parent.rb_left = tmp;
                        node.rb_right = parent;
                        if (tmp != null)
                        {
                            rb_set_parent_color(tmp, parent, RB_BLACK);
                        }
                        rb_set_parent_color(parent, node, RB_RED);
                        augment_rotate(parent, node);
                        parent = node;
                        tmp = node.rb_left;
                    }

                    /* Case 3 - left rotate at gparent */
                    gparent.rb_right = tmp; /* == parent->rb_left */
                    parent.rb_left = gparent;
                    if (tmp != null)
                    {
                        rb_set_parent_color(tmp, gparent, RB_BLACK);
                    }

                    __rb_rotate_set_parents(gparent, parent, root, RB_RED);
                    augment_rotate(gparent, parent);
                    break;
                }
            }
        }




    }
}
