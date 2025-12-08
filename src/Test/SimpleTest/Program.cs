using AKNet.Common;
using System;
using System.Net;

namespace AKNet.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetLog.AddConsoleLog();
            Console.WriteLine("Hello, World!");
            Draw(5);

            Span<byte> buffer1 = new byte[5] { 1, 2, 3, 4, 5};
            Span<byte> buffer2 = new byte[5] { 1, 2, 3, 3, 5 };

            if (buffer1.SequenceEqual(buffer2))
            {
                Console.WriteLine("==");
            }
            else
            {
                Console.WriteLine("!=");
            }

            //Dictionary<IPEndPoint, bool> mDic = new Dictionary<IPEndPoint, bool>();
            //mDic.Add(new IPEndPoint(IPAddress.Parse("192.168.0.1"), 1), true);
            //mDic.Add(new IPEndPoint(IPAddress.Parse("192.168.0.1"), 1), true);
            //mDic.Add(new IPEndPoint(IPAddress.Parse("192.168.0.1"), 1), true);

            ProfilerTool.TestStart();
            Random mRandom = new Random();
            for (int i = 0; i < 1000000; i++)
            {
                int A = mRandom.Next(0, int.MaxValue - 1);
                var mBuf1 = new byte[100];
                var mBuf2 = new byte[100];
                EndianBitConverter.SetBytes(mBuf1, 0, A);
            }
            ProfilerTool.TestFinishAndLog("EndianBitConverter");

            while(true)
            {

            }
        }

        private static void Draw(int N)
        {
            for (int i = 1; i <= N; i++)
            {
                List<int> mList = GetRowList(i);
                Console.WriteLine(string.Join(" ", mList));
            }
            
        }

        //private static string GetWhiteSpace(int N)
        //{
        //    string e
        //    for (int i = 1; i <= N; i++)
        //    {
                
        //    }

        //}

        private static List<int> GetRowList(int N)
        {
            if (N == 1)
            {
                return new List<int>() { 1 }; 
            }
            else
            {
                var mLastList = GetRowList(N - 1);
                var mNowList = new List<int>();
                mNowList.Add(1);
                for (int i = 0; i < mLastList.Count - 1; i++)
                {
                    mNowList.Add(mLastList[i] + mLastList[i + 1]);
                }
                mNowList.Add(1);
                return mNowList;
            }
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
