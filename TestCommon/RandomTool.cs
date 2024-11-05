namespace TestCommon
{
    public static class RandomTool
    {
        private static readonly Random mRandom = new Random((int)(DateTime.Now.Ticks % int.MaxValue));

        public static double Random()
        {
            return mRandom.NextDouble();
        }

        public static int RandomArrayIndex(int x, int y)
        {
            return mRandom.Next(x, y);
        }

        public static int Random(int x, int y)
        {
            return mRandom.Next(x, y + 1);
        }

        public static uint Random(uint x, uint y)
        {
            return (uint)mRandom.Next((int)x, (int)y + 1);
        }

        public static int Random(int x)
        {
            return mRandom.Next(x);
        }

        public static uint Random(uint x)
        {
            return (uint)mRandom.Next((int)x);
        }

        public static int GetIndexByRate(params int[] mRateList)
        {
            int nSumRate = 0;
            foreach (var nRate in mRateList)
            {
                nSumRate = nSumRate + nRate;
            }

            int nTempTargetRate = nSumRate + 1;
            if (nSumRate >= 1)
            {
                nTempTargetRate = Random(1, nSumRate);
            }

            int nTempRate = 0;
            int nTargetIndex = -1;
            for (int i = 0; i < mRateList.Length; i++)
            {
                nTempRate = nTempRate + mRateList[i];
                if (nTempRate >= nTempTargetRate)
                {
                    nTargetIndex = i;
                    break;
                }
            }

            return nTargetIndex;
        }
    }
}
