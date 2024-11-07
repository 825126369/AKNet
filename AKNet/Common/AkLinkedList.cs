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
    }
}
