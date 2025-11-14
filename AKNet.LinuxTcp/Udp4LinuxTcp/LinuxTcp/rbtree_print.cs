/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;

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
    internal static partial class LinuxTcpFunc
    {
#if DEBUG
        static string print_draw_rb_row_empty(int nRowIndex)
        {
            string empty = "";
            for (int i = 0; i < 10 - nRowIndex; i++)
            {
                empty += "   ";
            }
            return empty;
        }

        static string print_draw_rb_node(rb_node node)
        {
            string drawStr = "";
            if (node == null)
            {
                return drawStr;
            }

            drawStr += $"({TCP_SKB_CB(rb_entry(node)).seq}, {(rb_color(node) == RB_BLACK ? "黑" : "红")})";
            return drawStr;
        }

        static string print_draw_rb_row_node(int nRowIndex, List<rb_node> mRowRbNodeList)
        {
            string drawStr = print_draw_rb_row_empty(nRowIndex);
            for (int i = 0; i < mRowRbNodeList.Count; i++)
            {
                drawStr += print_draw_rb_node(mRowRbNodeList[i]) + "   ";
            }
            return drawStr;
        }

        static string print_draw_rb_row_xian(int nRowIndex, List<rb_node> mRowRbNodeList)
        {
            string drawStr = print_draw_rb_row_empty(nRowIndex);
            for (int i = 0; i < mRowRbNodeList.Count; i++)
            {
                if (mRowRbNodeList[i] != null)
                {
                    if (i % 2 == 0)
                    {
                        drawStr += "/" + "   ";
                    }
                    else
                    {
                        drawStr += "\\" + "   ";
                    }
                }
            }
            return drawStr;
        }

        static List<rb_node> mRowRbNodeList = new List<rb_node>();
        static void print_draw_rb_tree(rb_root root)
        {
            string drawStr = string.Empty;

            int nLeftHeight = 0;
            int nRightHeight = 0;
            rb_node node = root.rb_node;
            while (node != null)
            {
                if (node.rb_left != null)
                {
                    nLeftHeight++;
                    node = node.rb_left;
                    continue;
                }

                if (node.rb_right != null)
                {
                    nLeftHeight++;
                    node = node.rb_right;
                    continue;
                }

                break;
            }

            while (node != null)
            {
                if (node.rb_right != null)
                {
                    nRightHeight++;
                    node = node.rb_right;
                    continue;
                }

                if (node.rb_left != null)
                {
                    nRightHeight++;
                    node = node.rb_left;
                    continue;
                }

                break;
            }

            int nHeight = Math.Max(nLeftHeight, nRightHeight);
            mRowRbNodeList.Clear();
            int nRowIndex = 0;
            string empty = " ";
            while (nRowIndex < nHeight)
            {
                if (nRowIndex == 0)
                {
                    mRowRbNodeList.Add(root.rb_node);
                    drawStr += print_draw_rb_row_node(nRowIndex++, mRowRbNodeList);
                    drawStr += "\n";
                }
                else
                {
                    List<rb_node> mNew_RowRbNodeList = new List<rb_node>();
                    for (int i = 0; i < mRowRbNodeList.Count; i++)
                    {
                        if (mRowRbNodeList[i] != null)
                        {
                            mNew_RowRbNodeList.Add(mRowRbNodeList[i].rb_left);
                            mNew_RowRbNodeList.Add(mRowRbNodeList[i].rb_right);
                        }
                        else
                        {
                            mNew_RowRbNodeList.Add(null);
                            mNew_RowRbNodeList.Add(null);
                        }
                    }
                    mRowRbNodeList = mNew_RowRbNodeList;

                    drawStr += print_draw_rb_row_xian(nRowIndex++, mRowRbNodeList);
                    drawStr += "\n";
                    drawStr += print_draw_rb_row_node(nRowIndex++, mRowRbNodeList);
                    drawStr += "\n";
                }
            }

            NetLog.Log("\n" + drawStr);
        }
#endif

    }

}
