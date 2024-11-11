/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Generic;

namespace AKNet.Common
{
    internal class AkLinkedListNode<T> : IPoolItemInterface
    {
        public AkLinkedListNode<T> Previous = null;
        public AkLinkedListNode<T> Next = null;
        public T Value;

        public AkLinkedListNode()
        {
            this.Value = default;
        }

        public AkLinkedListNode(T value)
        {
            this.Value = value;
        }

        public void Reset()
        {
            Value = default;
            Next = null;
            Previous = null;
        }
    }

    internal class AkLinkedList<T>
    {
        public AkLinkedListNode<T> First = null;
        public AkLinkedListNode<T> Last = null;
        private readonly ObjectPool<AkLinkedListNode<T>> mNodePool = new ObjectPool<AkLinkedListNode<T>>();
        public int Count;

        public AkLinkedList(int nNodePoolMaxCapacity)
        {
            SetNodePoolMaxCapacity(nNodePoolMaxCapacity);
        }

        public void SetNodePoolMaxCapacity(int nCapacity)
        {
            mNodePool.SetMaxCapacity(nCapacity);
        }

        public void AddLast(T value)
        {
            var mNode = mNodePool.Pop();
            mNode.Value = value;

            if(First == null)
            {
                First = mNode;
                Last = mNode;
            }
            else
            {
                Last.Next = mNode;
                mNode.Previous = Last;
                Last = mNode;
            }

            Count++;
        }

        public void Clear()
        {
            var mNode = First;
            while (mNode != null)
            {
                var mNextNode = mNode.Next;
                mNodePool.recycle(mNode);
                mNode = mNextNode;
            }

            First = null;
            Last = null;
            Count = 0;
        }

        public void Remove(AkLinkedListNode<T> mNode)
        {
            if (mNode.Previous != null)
            {
                mNode.Previous.Next = mNode.Next;
            }
            else
            {
                First = mNode.Next;
            }

            if (mNode.Next != null)
            {
                mNode.Next.Previous = mNode.Previous;
            }
            else
            {
                Last = mNode.Previous;
            }

            mNodePool.recycle(mNode);
            Count--;
        }

        public void RemoveFirst()
        {
            if (First == null) return;
            var mRemoveNode = First;

            if (mRemoveNode.Next != null)
            {
                mRemoveNode.Next.Previous = null;
            }
            else
            {
                Last = mRemoveNode.Previous;
            }

            First = First.Next;

            mNodePool.recycle(mRemoveNode);
            Count--;
        }

        public bool Contains(T Value)
        {
            EqualityComparer<T> c = EqualityComparer<T>.Default;

            var mNode = First;
            while (mNode != null)
            {
                if (c.Equals(mNode.Value, Value))
                {
                    return true;
                }
                mNode = mNode.Next;
            }
            return false;
        }
    }
}
