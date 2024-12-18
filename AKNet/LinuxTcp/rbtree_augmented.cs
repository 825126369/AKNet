using System;
using System.Xml.Linq;

namespace AKNet.LinuxTcp
{
    public class rb_augment_callbacks
    {
        public Action<rb_node, rb_node> propagate = null;
        public Action<rb_node, rb_node> copy = null;
        public Action<rb_node, rb_node> rotate = null;
    }

    internal static partial class LinuxTcpFunc
    {
        public static int rb_color(rb_node rb)
        {
            return rb.__color;
        }

        public static bool rb_is_red(rb_node rb)
        {
            return rb.__color == rb_node.RB_RED;
        }

        public static bool rb_is_black(rb_node rb)
        {
            return rb.__color == rb_node.RB_BLACK;
        }

        public static void rb_set_parent(rb_node rb, rb_node p)
        {
            rb.__rb_parent = p;
            rb.__color = rb_color(rb);
        }

        public static void rb_set_parent_color(rb_node rb, rb_node p, int color)
        {
            rb.__rb_parent = p;
            rb.__color = color;
        }

        public static void __rb_change_child(rb_node old, rb_node newNode, rb_node parent, rb_root root)
        {
            if (parent != null)
            {
                if (parent.rb_left == old)
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

        public static rb_node __rb_erase_augmented(rb_node node, rb_root root, rb_augment_callbacks augment)
        {
            rb_node child = node.rb_right;
            rb_node tmp = node.rb_left;
            rb_node parent, rebalance;

            rb_node pc_parent;
            int pc_color;

            if (tmp == null)
            {
                pc_parent = node.__rb_parent;
                pc_color = node.__color;
                parent = rb_parent(node);
                __rb_change_child(node, child, parent, root);
                if (child != null)
                {
                    child.__rb_parent = pc_parent;
                    child.__color = pc_color;
                    rebalance = null;
                }
                else
                {
                    rebalance = rb_is_black(node) ? parent : null;
                }
                tmp = parent;
            }
            else if (child == null)
            {
                tmp.__rb_parent = pc_parent;
                tmp.__color = pc_color;

                parent = rb_parent(node);
                __rb_change_child(node, tmp, parent, root);
                rebalance = null;
                tmp = parent;
            }
            else
            {
                rb_node successor = child;
                rb_node child2;

                tmp = child.rb_left;
                if (tmp == null)
                {
                    /*
                     * Case 2: node's successor is its right child
                     *
                     *    (n)          (s)
                     *    / \          / \
                     *  (x) (s)  ->  (x) (c)
                     *        \
                     *        (c)
                     */
                    parent = successor;
                    child2 = successor.rb_right;

                    augment.copy(node, successor);
                }
                else
                {
                    /*
                     * Case 3: node's successor is leftmost under
                     * node's right child subtree
                     *
                     *    (n)          (s)
                     *    / \          / \
                     *  (x) (y)  ->  (x) (y)
                     *      /            /
                     *    (p)          (p)
                     *    /            /
                     *  (s)          (c)
                     *    \
                     *    (c)
                     */
                    do
                    {
                        parent = successor;
                        successor = tmp;
                        tmp = tmp.rb_left;
                    } while (tmp != null);

                    child2 = successor.rb_right;
                    parent.rb_left = child2;
                    successor.rb_right = child;
                    rb_set_parent(child, successor);

                    augment.copy(node, successor);
                    augment.propagate(parent, successor);
                }

                tmp = node.rb_left;
                successor.rb_left = tmp;
                rb_set_parent(tmp, successor);

                pc_parent = node.__rb_parent;
                pc_color = node.__color;

                tmp = rb_parent(node);
                __rb_change_child(node, successor, tmp, root);

                if (child2 != null)
                {
                    rb_set_parent_color(child2, parent, rb_node.RB_BLACK);
                    rebalance = null;
                }
                else
                {
                    rebalance = rb_is_black(successor) ? parent : null;
                }
                successor.__rb_parent = pc_parent;
                successor.__color = pc_color;
                tmp = successor;
            }

            augment.propagate(tmp, null);
            return rebalance;
        }


        public static void rb_erase_augmented(rb_node node, rb_root root, rb_augment_callbacks augment)
        {
            rb_node rebalance = __rb_erase_augmented(node, root, augment);
            if (rebalance != null)
            {
                __rb_erase_color(rebalance, root, augment.rotate);
            }
        }

        public static void rb_erase_augmented_cached(rb_node node, rb_root_cached root, rb_augment_callbacks augment)
        {
            if (root.rb_leftmost == node)
            {
                root.rb_leftmost = rb_next(node);
            }
            rb_erase_augmented(node, root.rb_root, augment);
        }

    }
}
