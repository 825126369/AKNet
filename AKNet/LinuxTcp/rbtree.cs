using System;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace AKNet.LinuxTcp
{
	internal static partial class LinuxTcpFunc
	{

		public static rb_node rb_parent(rb_node r)
		{
			return r.__rb_parent;
		}

		public static rb_node rb_entry(rb_node r)
		{
			return r.__rb_parent;
		}

		public static bool RB_EMPTY_ROOT(rb_root root)
		{
			return root.rb_node == null;
		}

		public static bool RB_EMPTY_NODE(rb_node node)
		{
			return node.__rb_parent == null;
		}

		public static rb_node rb_first(rb_root root)
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

		public static rb_node rb_last(rb_root root)
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

		public static rb_node rb_next(rb_node node)
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

		public static rb_node rb_prev(rb_node node)
		{
			rb_node parent = null;
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

		public static void rb_replace_node(rb_node victim, rb_node newNode, rb_root root)
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

		public static rb_node rb_left_deepest_node(rb_node node)
		{
			for (; ; )
			{
				if (node.rb_left != null)
				{
					node = node.rb_left;
				}
				else if (node.rb_right != null)
				{
					node = node.rb_right;
				}
				else
				{
					return node;
				}
			}
		}

		public static rb_node rb_next_postorder(rb_node node)
		{
			rb_node parent = null;
			if (node == null)
			{
				return null;
			}
			parent = rb_parent(node);

			if (parent != null && node == parent.rb_left && parent.rb_right != null)
			{
				return rb_left_deepest_node(parent.rb_right);
			}
			else
			{
				return parent;
			}
		}

		public static rb_node rb_first_postorder(rb_root root)
		{
			if (root.rb_node == null)
			{
				return null;
			}
			return rb_left_deepest_node(root.rb_node);
		}



		
		public static void rb_set_black(rb_node rb)
		{
            rb.__color += rb_node.RB_BLACK;
        }

        public static rb_node rb_red_parent(rb_node red)
		{
			return red.__rb_parent;
		}

		public static void __rb_rotate_set_parents(rb_node old, rb_node newNode, rb_root root, int color)
		{
			rb_node parent = rb_parent(old);
			newNode.__rb_parent = old.__rb_parent;
			newNode.__color = old.__color;
			rb_set_parent_color(old, newNode, color);
			__rb_change_child(old, newNode, parent, root);
		}
		
		public static void __rb_erase_color(rb_node parent, rb_root root, Action<rb_node, rb_node> augment_rotate)
		{
			____rb_erase_color(parent, root, augment_rotate);
		}

		public static void ____rb_erase_color(rb_node parent, rb_root root, Action<rb_node, rb_node> augment_rotate)
		{
			rb_node node = null, sibling, tmp1, tmp2;

			while (true)
			{
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
						rb_set_parent_color(tmp1, parent, rb_node.RB_BLACK);
						__rb_rotate_set_parents(parent, sibling, root, rb_node.RB_RED);
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
							rb_set_parent_color(sibling, parent, rb_node.RB_RED);
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
							rb_set_parent_color(tmp1, sibling, rb_node.RB_BLACK);
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
					rb_set_parent_color(tmp1, sibling, rb_node.RB_BLACK);
					if (tmp2 != null)
					{
						rb_set_parent(tmp2, parent);
					}
					__rb_rotate_set_parents(parent, sibling, root, rb_node.RB_BLACK);
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
						rb_set_parent_color(tmp1, parent, rb_node.RB_BLACK);
						__rb_rotate_set_parents(parent, sibling, root, rb_node.RB_RED);
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
							rb_set_parent_color(sibling, parent, rb_node.RB_RED);
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
							rb_set_parent_color(tmp1, sibling, rb_node.RB_BLACK);
						}
						augment_rotate(sibling, tmp2);
						tmp1 = sibling;
						sibling = tmp2;
					}
					/* Case 4 - right rotate at parent + color flips */
					tmp2 = sibling.rb_right;
					parent.rb_left = tmp2;
					sibling.rb_right = parent;
					rb_set_parent_color(tmp1, sibling, rb_node.RB_BLACK);
					if (tmp2 != null)
					{
						rb_set_parent(tmp2, parent);
					}
					__rb_rotate_set_parents(parent, sibling, root, rb_node.RB_BLACK);
					augment_rotate(parent, sibling);
					break;
				}
			}
		}

		public static void __rb_insert(rb_node node, rb_root root, Action<rb_node, rb_node> augment_rotate)
		{
			rb_node parent = rb_red_parent(node), gparent, tmp;
			while (true)
			{
				/*
				 * Loop invariant: node is red.
				 */
				if (parent == null)
				{
					/*
					 * The inserted node is root. Either this is the
					 * first node, or we recursed at Case 1 below and
					 * are no longer violating 4).
					 */
					rb_set_parent_color(node, null, rb_node.RB_BLACK);
					break;
				}

				/*
				 * If there is a black parent, we are done.
				 * Otherwise, take some corrective action as,
				 * per 4), we don't want a red root or two
				 * consecutive red nodes.
				 */
				if (rb_is_black(parent))
					break;

				gparent = rb_red_parent(parent);

				tmp = gparent.rb_right;
				if (parent != tmp)
				{    /* parent == gparent->rb_left */
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
						rb_set_parent_color(tmp, gparent, rb_node.RB_BLACK);
						rb_set_parent_color(parent, gparent, rb_node.RB_BLACK);
						node = gparent;
						parent = rb_parent(node);
						rb_set_parent_color(node, parent, rb_node.RB_RED);
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
							rb_set_parent_color(tmp, parent, rb_node.RB_BLACK);
						}
						rb_set_parent_color(parent, node, rb_node.RB_RED);
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
						rb_set_parent_color(tmp, gparent, rb_node.RB_BLACK);
					}
					__rb_rotate_set_parents(gparent, parent, root, rb_node.RB_RED);
					augment_rotate(gparent, parent);
					break;
				}
				else
				{
					tmp = gparent.rb_left;
					if (tmp != null && rb_is_red(tmp))
					{
						/* Case 1 - color flips */
						rb_set_parent_color(tmp, gparent, rb_node.RB_BLACK);
						rb_set_parent_color(parent, gparent, rb_node.RB_BLACK);
						node = gparent;
						parent = rb_parent(node);
						rb_set_parent_color(node, parent, rb_node.RB_RED);
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
							rb_set_parent_color(tmp, parent, rb_node.RB_BLACK);
						}
						rb_set_parent_color(parent, node, rb_node.RB_RED);
						augment_rotate(parent, node);
						parent = node;
						tmp = node.rb_left;
					}

					/* Case 3 - left rotate at gparent */
					gparent.rb_right = tmp; /* == parent->rb_left */
					parent.rb_left = gparent;
					if (tmp != null)
					{
						rb_set_parent_color(tmp, gparent, rb_node.RB_BLACK);
					}
					__rb_rotate_set_parents(gparent, parent, root, rb_node.RB_RED);
					augment_rotate(gparent, parent);
					break;
				}
			}
		}

	}
}
