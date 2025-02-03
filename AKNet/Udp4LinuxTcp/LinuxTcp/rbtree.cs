using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    /*
        红黑树（Red-Black Tree）是一种自平衡的二叉搜索树，它在每个节点上增加了一个存储位来表示节点的颜色，可以是红色或黑色。
        通过对任何一条从根到叶子的路径上节点的颜色进行约束，红黑树确保树的平衡，从而保证了基本操作（如查找、插入和删除）的时间复杂度为 O(log n)。
        
        红黑树的性质
        红黑树具有以下五个基本性质：
        1: 节点是红色或黑色：每个节点都有一个颜色属性，红色或黑色。
        2: 根节点是黑色：树的根节点必须是黑色。
        3: 叶子节点是黑色：叶子节点（即空节点或 NULL 节点）是黑色。
        4: 红色节点的子节点是黑色：如果一个节点是红色，则它的两个子节点都是黑色。
        5: 从任何节点到其每个叶子节点的所有路径都包含相同数量的黑色节点。
        为什么需要红黑树
        红黑树是一种自平衡的二叉搜索树，它通过颜色约束来确保树的平衡。
        与 AVL 树相比，红黑树在插入和删除操作中进行的调整较少，因此在实际应用中，红黑树的插入和删除操作通常比 AVL 树更快。
        红黑树在许多编程语言的标准库中被广泛使用，例如 C++ 的 std::map 和 std::set，以及 Linux 内核中的内存管理、文件系统等。
        红黑树的操作
        查找：与普通二叉搜索树的查找操作相同，时间复杂度为 O(log n)。
        插入：插入新节点后，需要进行颜色调整和旋转操作，以保持红黑树的性质。
        删除：删除节点后，也需要进行颜色调整和旋转操作，以保持红黑树的性质。
     */

    internal class rb_node
    {
        public readonly sk_buff value;
        public byte color;
        public rb_node parent;
        public rb_node rb_right;
        public rb_node rb_left;

        public rb_node(sk_buff skb)
        {
            this.value = skb;
            this.Reset();
        }

        public void Reset()
        {
            color = LinuxTcpFunc.RB_RED;
            parent = null;
            rb_right = null;
            rb_left = null;
        }
    }

    internal class rb_root
    {
        public rb_node rb_node = null;
    }

    internal class rb_augment_callbacks
    {
        public Action<rb_node, rb_node> propagate;
        public Action<rb_node, rb_node> copy;
        public Action<rb_node, rb_node> rotate;
    }

    internal static partial class LinuxTcpFunc
    {
        public const byte RB_RED = 0;
        public const byte RB_BLACK = 1;

        static void dummy_propagate(rb_node node, rb_node stop) { }
        static void dummy_copy(rb_node oldNode, rb_node newNode) { }
        static void dummy_rotate(rb_node oldNode, rb_node newNode) { }

        static readonly rb_augment_callbacks dummy_callbacks = new rb_augment_callbacks()
        {
            propagate = dummy_propagate,
            copy = dummy_copy,
            rotate = dummy_rotate
        };


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
            if (node != null)
            {
                return node.value;
            }
            return null;
        }

        static bool RB_EMPTY_ROOT(rb_root node)
        {
            return node.rb_node == null;
        }

        static bool RB_EMPTY_NODE(rb_node node)
        {
            return node.parent == null;
        }

        static void RB_CLEAR_NODE(rb_node node)
        {
            node.parent = null;
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

        static int rb_count(rb_root root)
        {
            int nCount = 0;
            for (var node = rb_first(root); node != null; node = rb_next(node))
            {
                nCount++;
            }
            return nCount;
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

        //恢复平衡：在删除节点后，调整红黑树的结构和颜色，确保树的平衡。
        //回调旋转：调用用户提供的回调函数，处理旋转操作。
        static void ____rb_erase_color(rb_node parent, rb_root root, Action<rb_node, rb_node> augment_rotate)
        {
            rb_node node = null, sibling, tmp1, tmp2;

            while (true)
            {
                /*
		         * Loop invariants:
		         * - node is black (or NULL on first iteration)
		         * - node is not the root (parent is not NULL)
		         * - All leaf paths going through parent and node have a
		         *   black node count that is 1 lower than other leaf paths.
		         */
                sibling = parent.rb_right;
                if (node != sibling)
                {
                    /* node == parent->rb_left */
                    if (rb_is_red(sibling))
                    {
                        /*
				         * Case 1 - left rotate at parent
				         *
				         *     P               S
				         *    / \             / \
				         *   N   s    -->    p   Sr
				         *      / \         / \
				         *     Sl  Sr      N   Sl
				         */
                        tmp1 = sibling.rb_left;
                        parent.rb_right = tmp1;
                        sibling.rb_left = parent;
                        rb_set_parent_color(tmp1, parent, RB_BLACK);
                        __rb_rotate_set_parents(parent, sibling, root, RB_RED);
                        augment_rotate(parent, sibling);
                        sibling = tmp1;
                    }
                    tmp1 = sibling.rb_right;
                    if (tmp1 == null || rb_is_black(tmp1))
                    {
                        tmp2 = sibling.rb_left;
                        if (tmp2 == null || rb_is_black(tmp2))
                        {
                            /*
					         * Case 2 - sibling color flip
					         * (p could be either color here)
					         *
					         *    (p)           (p)
					         *    / \           / \
					         *   N   S    -->  N   s
					         *      / \           / \
					         *     Sl  Sr        Sl  Sr
					         *
					         * This leaves us violating 5) which
					         * can be fixed by flipping p to black
					         * if it was red, or by recursing at p.
					         * p is red when coming from Case 1.
					         */
                            rb_set_parent_color(sibling, parent, RB_RED);
                            if (rb_is_red(parent))
                            {
                                rb_set_black(parent);
                            }
                            else
                            {
                                node = parent;
                                parent = rb_parent(node);
                                if (parent != null)
                                {
                                    continue;
                                }
                            }
                            break;
                        }

                        /*
				         * Case 3 - right rotate at sibling
				         * (p could be either color here)
				         *
				         *   (p)           (p)
				         *   / \           / \
				         *  N   S    -->  N   sl
				         *     / \             \
				         *    sl  sr            S
				         *                       \
				         *                        sr
				         *
				         * Note: p might be red, and then both
				         * p and sl are red after rotation(which
				         * breaks property 4). This is fixed in
				         * Case 4 (in __rb_rotate_set_parents()
				         *         which set sl the color of p
				         *         and set p RB_BLACK)
				         *
				         *   (p)            (sl)
				         *   / \            /  \
				         *  N   sl   -->   P    S
				         *       \        /      \
				         *        S      N        sr
				         *         \
				         *          sr
				         */
                        tmp1 = tmp2.rb_right;
                        sibling.rb_left = tmp1;
                        tmp2.rb_right = sibling;
                        parent.rb_right = tmp2;
                        if (tmp1 != null)
                        {
                            rb_set_parent_color(tmp1, sibling, RB_BLACK);
                        }
                        augment_rotate(sibling, tmp2);
                        tmp1 = sibling;
                        sibling = tmp2;
                    }
                    /*
			         * Case 4 - left rotate at parent + color flips
			         * (p and sl could be either color here.
			         *  After rotation, p becomes black, s acquires
			         *  p's color, and sl keeps its color)
			         *
			         *      (p)             (s)
			         *      / \             / \
			         *     N   S     -->   P   Sr
			         *        / \         / \
			         *      (sl) sr      N  (sl)
			         */
                    tmp2 = sibling.rb_left;
                    parent.rb_right = tmp2;
                    sibling.rb_left = parent;
                    rb_set_parent_color(tmp1, sibling, RB_BLACK);
                    if (tmp2 != null)
                    {
                        rb_set_parent(tmp2, parent);
                    }
                    __rb_rotate_set_parents(parent, sibling, root, RB_BLACK);
                    augment_rotate(parent, sibling);
                    break;
                }
                else
                {
                    sibling = parent.rb_left;
                    if (rb_is_red(sibling))
                    {
                        /* Case 1 - right rotate at parent */
                        tmp1 = sibling.rb_right;
                        parent.rb_left = tmp1;
                        sibling.rb_right = parent;
                        rb_set_parent_color(tmp1, parent, RB_BLACK);
                        __rb_rotate_set_parents(parent, sibling, root, RB_RED);
                        augment_rotate(parent, sibling);
                        sibling = tmp1;
                    }

                    tmp1 = sibling.rb_left;
                    if (tmp1 == null || rb_is_black(tmp1))
                    {
                        tmp2 = sibling.rb_right;
                        if (tmp2 == null || rb_is_black(tmp2))
                        {
                            /* Case 2 - sibling color flip */
                            rb_set_parent_color(sibling, parent, RB_RED);
                            if (rb_is_red(parent))
                            {
                                rb_set_black(parent);
                            }
                            else
                            {
                                node = parent;
                                parent = rb_parent(node);
                                if (parent != null)
                                {
                                    continue;
                                }
                            }
                            break;
                        }
                        /* Case 3 - left rotate at sibling */
                        tmp1 = tmp2.rb_left;
                        sibling.rb_right = tmp1;
                        tmp2.rb_left = sibling;
                        parent.rb_left = tmp2;
                        if (tmp1 != null)
                        {
                            rb_set_parent_color(tmp1, sibling, RB_BLACK);
                        }
                        augment_rotate(sibling, tmp2);
                        tmp1 = sibling;
                        sibling = tmp2;
                    }

                    /* Case 4 - right rotate at parent + color flips */
                    tmp2 = sibling.rb_right;
                    parent.rb_left = tmp2;
                    sibling.rb_right = parent;
                    rb_set_parent_color(tmp1, sibling, RB_BLACK);
                    if (tmp2 != null)
                    {
                        rb_set_parent(tmp2, parent);
                    }
                    __rb_rotate_set_parents(parent, sibling, root, RB_BLACK);
                    augment_rotate(parent, sibling);
                    break;
                }
            }
        }

        //用于从红黑树中删除节点，并处理增强型红黑树的回调操作。
        static rb_node __rb_erase_augmented(rb_node node, rb_root root, rb_augment_callbacks augment)
        {
            rb_node child = node.rb_right;
            rb_node tmp = node.rb_left;
            rb_node parent, rebalance;

            rb_node pc_parent = null;
            byte pc_color = RB_BLACK;

            if (tmp == null)
            {
                pc_parent = node.parent;
                pc_color = node.color;

                __rb_change_child(node, child, pc_parent, root);
                if (child != null)
                {
                    child.parent = pc_parent;
                    child.color = pc_color;
                    rebalance = null;
                }
                else
                {
                    rebalance = rb_is_black(node) ? pc_parent : null;
                }
                tmp = pc_parent;
            }
            else if (child == null)
            {
                pc_parent = node.parent;
                pc_color = node.color;
                tmp.parent = pc_parent;
                tmp.color = pc_color;

                __rb_change_child(node, tmp, pc_parent, root);
                rebalance = null;
                tmp = pc_parent;
            }
            else
            {

                rb_node successor = child, child2;
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

                pc_parent = node.parent;
                pc_color = node.color;

                tmp = pc_parent;
                __rb_change_child(node, successor, tmp, root);

                if (child2 != null)
                {
                    rb_set_parent_color(child2, parent, RB_BLACK);
                    rebalance = null;
                }
                else
                {
                    rebalance = rb_is_black(successor) ? parent : null;
                }

                successor.parent = pc_parent;
                successor.color = pc_color;

                tmp = successor;
            }

            augment.propagate(tmp, null);
            return rebalance;
        }

        static void rb_replace_node(rb_node victim, rb_node newNode, rb_root root)
        {
            rb_node parent = rb_parent(victim);
            newNode = victim;

            if (victim.rb_left != null)
            {
                rb_set_parent(victim.rb_left, newNode);
            }

            if (victim.rb_right != null)
            {
                rb_set_parent(victim.rb_right, newNode);
            }
            __rb_change_child(victim, newNode, parent, root);
        }

        static rb_node rb_first(rb_root root)
        {
            rb_node n = root.rb_node;
            if (n == null)
            {
                return null;
            }
            while (n.rb_left != null)
            {
                n = n.rb_left;
            }
            return n;
        }

        static rb_node rb_last(rb_root root)
        {
            rb_node n = root.rb_node;
            if (n == null)
            {
                return null;
            }

            while (n.rb_right != null)
            {
                n = n.rb_right;
            }
            return n;
        }

        static rb_node rb_next(rb_node node)
        {
            rb_node parent = null;
            if (RB_EMPTY_NODE(node))
            {
                return null;
            }

            if (node.rb_right != null)
            {
                node = node.rb_right;
                while (node.rb_left != null)
                {
                    node = node.rb_left;
                }
                return node;
            }

            while ((parent = rb_parent(node)) != null && node == parent.rb_right)
            {
                node = parent;
            }
            return parent;
        }

        static rb_node rb_prev(rb_node node)
        {
            rb_node parent;
            if (RB_EMPTY_NODE(node))
            {
                return null;
            }

            if (node.rb_left != null)
            {
                node = node.rb_left;
                while (node.rb_right != null)
                {
                    node = node.rb_right;
                }
                return node;
            }

            while ((parent = rb_parent(node)) != null && node == parent.rb_left)
            {
                node = parent;
            }
            return parent;
        }

        //rb_link_node 是一个基础函数，用于将新节点链接到红黑树的指定位置。
        //它不直接设置节点的颜色，而是为后续的颜色设置和树的平衡操作做好准备。
        //颜色的设置通常在 rb_insert_color 函数中完成。
        static void rb_link_node(rb_node node, rb_node parent, out rb_node rb_link)
        {
            node.parent = parent;
            node.color = RB_RED;
            node.rb_left = node.rb_right = null;
            rb_link = node;
        }

        static void rb_insert_color(rb_node node, rb_root root)
        {
            __rb_insert(node, root, dummy_rotate);
        }

        static void rb_erase(rb_node node, rb_root root)
        {
            rb_node rebalance;
            rebalance = __rb_erase_augmented(node, root, dummy_callbacks);
            if (rebalance != null)
            {
                ____rb_erase_color(rebalance, root, dummy_rotate);
            }
        }

    }

}
