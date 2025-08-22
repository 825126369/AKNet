namespace AKNet.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            List<int> mList = new List<int>() { 2, 1, 5, 3, 2, 0,8, 10,0,1 };
            QuicSort(mList);
            Console.WriteLine(string.Join(',', mList));
        }

        private static void QuicSort(List<int> AList)
        {
            QuicSort(AList, 0, AList.Count - 1);
        }

        private static void QuicSort(List<int> AList, int nBeginIndex, int nEndIndex)
        {
            int Key = AList[nBeginIndex];
            int i = nBeginIndex;
            int j = nEndIndex;

            if (nBeginIndex >= nEndIndex)
            {
                return;
            }

            while (i < j)
            {
                while (AList[j] >= Key && j > i)
                {
                    j--;
                }

                if (AList[j] < Key)
                {
                    AList[i] = AList[j];
                }

                while (AList[i] <= Key && i < j)
                {
                    i++;
                }

                if (AList[i] > Key)
                {
                    AList[j] = AList[i];
                }
            }

            AList[i] = Key;
            QuicSort(AList, nBeginIndex, i - 1);
            QuicSort(AList, i + 1, nEndIndex);
        }
    }
}
