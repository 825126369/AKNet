using AKNet.Common;
using MSQuic1;

namespace MSTest
{
    public class SmartRange
    {
        readonly QUIC_RANGE range = new QUIC_RANGE();
        public SmartRange(int MaxAllocSize = MSQuicFunc.QUIC_MAX_RANGE_ALLOC_SIZE)
        {
            MSQuicFunc.QuicRangeInitialize(MaxAllocSize, range);
        }

        ~SmartRange()
        {
            MSQuicFunc.QuicRangeUninitialize(range);
        }

        public void Reset()
        {
            MSQuicFunc.QuicRangeReset(range);
        }

        public bool TryAdd(long value)
        {
            return MSQuicFunc.QuicRangeAddValue(range, value) != false;
        }

        public bool TryAdd(long low, long count)
        {
            bool rangeUpdated;
            return MSQuicFunc.QuicRangeAddRange(range, low, count, out rangeUpdated) != null;
        }

        public void Add(long value)
        {
            Assert.IsTrue(TryAdd(value));
        }

        public void Add(long low, long count)
        {
            Assert.IsTrue(TryAdd(low, count));
        }

        public void Remove(long low, long count)
        {
            Assert.IsTrue(MSQuicFunc.QuicRangeRemoveRange(range, low, count));
        }

        public int Find(long value)
        {
            QUIC_RANGE_SEARCH_KEY Key = new QUIC_RANGE_SEARCH_KEY() {  Low = value,  High = value };
            return MSQuicFunc.QuicRangeSearch(range, Key);
        }

        public int FindRange(long value, long count)
        {
            QUIC_RANGE_SEARCH_KEY Key = new QUIC_RANGE_SEARCH_KEY(){  Low = value,  High = value + count - 1 };
            return MSQuicFunc.QuicRangeSearch(range, Key);
        }

        public long Min()
        {
            long value;
            Assert.AreEqual(true, MSQuicFunc.QuicRangeGetMinSafe(range, out value));
            return value;
        }

        public long Max()
        {
            long value;
            Assert.AreEqual(true, MSQuicFunc.QuicRangeGetMaxSafe(range, out value));
            return value;
        }

        public int ValidCount()
        {
            return MSQuicFunc.QuicRangeSize(range);
        }
    }

    [TestClass]
    public class RangeTest
    {
        [TestMethod("RangeTest AddSingle")]
        public void TestMethod1()
        {
            SmartRange range = new SmartRange();
            range.Add(100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 100);
        }

        [TestMethod("RangeTest AddTwoAdjacentBefore")]
        public void TestMethod2()
        {
            SmartRange range = new SmartRange();
            range.Add(101);
            range.Add(100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 101);
        }

        [TestMethod("RangeTest AddTwoAdjacentAfter")]
        public void TestMethod3()
        {
            SmartRange range = new SmartRange();
            range.Add(100);
            range.Add(101);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 101);
        }

        [TestMethod("RangeTest AddTwoSeparateBefore")]
        public void TestMethod4()
        {
            SmartRange range = new SmartRange();
            range.Add(102);
            range.Add(100);
            Assert.AreEqual(range.ValidCount(), 2);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 102);
        }

        [TestMethod("RangeTest AddTwoSeparateAfter")]
        public void TestMethod5()
        {
            SmartRange range = new SmartRange();
            range.Add(100);
            range.Add(102);
            Assert.AreEqual(range.ValidCount(), 2);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 102);
        }

        [TestMethod("RangeTest AddThreeMerge")]
        public void TestMethod6()
        {
            SmartRange range = new SmartRange();
            range.Add(100);
            range.Add(102);
            range.Add(101);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 102);
        }

        [TestMethod("RangeTest AddBetween")]
        public void TestMethod7()
        {
            SmartRange range = new SmartRange();
            range.Add(100);
            range.Add(104);
            range.Add(102);
            Assert.AreEqual(range.ValidCount(), 3);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 104);
        }

        [TestMethod("RangeTest AddRangeSingle")]
        public void TestMethod8()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
        }

        [TestMethod("RangeTest AddRangeBetween")]
        public void TestMethod9()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 50);
            range.Add(300, 50);
            range.Add(200, 50);
            Assert.AreEqual(range.ValidCount(), 3);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 349);
        }

        [TestMethod("RangeTest AddRangeTwoAdjacentBefore")]
        public void TestMethod10()
        {
            SmartRange range = new SmartRange();
            range.Add(200, 100);
            range.Add(100, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 299);
        }

        [TestMethod("RangeTest AddRangeTwoAdjacentAfter")]
        public void TestMethod11()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            range.Add(200, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 299);
        }

        [TestMethod("RangeTest AddRangeTwoSeparateBefore")]
        public void TestMethod12()
        {
            SmartRange range = new SmartRange();
            range.Add(300, 100);
            range.Add(100, 100);
            Assert.AreEqual(range.ValidCount(), 2);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 399);
        }

        [TestMethod("RangeTest AddRangeTwoSeparateAfter")]
        public void TestMethod13()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            range.Add(300, 100);
            Assert.AreEqual(range.ValidCount(), 2);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 399);
        }

        [TestMethod("RangeTest AddRangeTwoOverlapBefore1")]
        public void TestMethod14()
        {
            SmartRange range = new SmartRange();
            range.Add(200, 100);
            range.Add(100, 150);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 299);
        }

        [TestMethod("RangeTest AddRangeTwoOverlapBefore2")]
        public void TestMethod15()
        {
            SmartRange range = new SmartRange();
            range.Add(200, 100);
            range.Add(100, 200);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 299);
        }

        [TestMethod("RangeTest AddRangeTwoOverlapBefore3")]
        public void TestMethod16()
        {
            SmartRange range = new SmartRange();
            range.Add(200, 50);
            range.Add(100, 200);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 299);
        }

        [TestMethod("RangeTest AddRangeTwoOverlapAfter1")]
        public void TestMethod17()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            range.Add(150, 150);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 299);
        }

        [TestMethod("RangeTest AddRangeTwoOverlapAfter2")]
        public void TestMethod18()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            range.Add(100, 200);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 299);
        }

        [TestMethod("RangeTest AddRangeThreeMerge")]
        public void TestMethod19()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            range.Add(300, 100);
            range.Add(200, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 399);
        }

        [TestMethod("RangeTest AddRangeThreeOverlapAndAdjacentAfter1")]
        public void TestMethod20()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 1);
            range.Add(200, 100);
            range.Add(101, 150);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 299);
        }

        [TestMethod("RangeTest AddRangeThreeOverlapAndAdjacentAfter2")]
        public void TestMethod21()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 1);
            range.Add(200, 100);
            range.Add(101, 299);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 399);
        }

        [TestMethod("RangeTest AddRangeThreeOverlapAndAdjacentAfter3")]
        public void TestMethod22()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            range.Add(300, 100);
            range.Add(150, 150);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 399);
        }

        [TestMethod("RangeTest AddRangeThreeOverlapAndAdjacentAfter4")]
        public void TestMethod23()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            range.Add(300, 100);
            range.Add(50, 250);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 50);
            Assert.AreEqual(range.Max(), 399);
        }

        [TestMethod("RangeTest RemoveRangeBefore")]
        public void TestMethod24()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
            range.Remove(0, 99);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
            range.Remove(0, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
        }

        [TestMethod("RangeTest RemoveRangeAfter")]
        public void TestMethod25()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
            range.Remove(201, 99);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
            range.Remove(200, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
        }

        [TestMethod("RangeTest RemoveRangeFront")]
        public void TestMethod26()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
            range.Remove(100, 20);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 120);
            Assert.AreEqual(range.Max(), 199);
        }

        [TestMethod("RangeTest RemoveRangeBack")]
        public void TestMethod27()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
            range.Remove(180, 20);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 179);
        }

        [TestMethod("RangeTest RemoveRangeAll")]
        public void TestMethod28()
        {
            SmartRange range = new SmartRange();
            range.Add(100, 100);
            Assert.AreEqual(range.ValidCount(), 1);
            Assert.AreEqual(range.Min(), 100);
            Assert.AreEqual(range.Max(), 199);
            range.Remove(100, 100);
            Assert.AreEqual(range.ValidCount(), 0);
        }

        [TestMethod("RangeTest ExampleAckTest")]
        public void TestMethod29()
        {
            SmartRange range = new SmartRange();
            range.Add(10000);
            range.Add(10001);
            range.Add(10003);
            range.Add(10002);
            Assert.AreEqual(range.ValidCount(), 1);
            range.Remove(10000, 2);
            Assert.AreEqual(range.ValidCount(), 1);
            range.Remove(10000, 4);
            Assert.AreEqual(range.ValidCount(), 0);
            range.Add(10005);
            range.Add(10006);
            range.Add(10004);
            range.Add(10007);
            Assert.AreEqual(range.ValidCount(), 1);
            range.Remove(10005, 2);
            Assert.AreEqual(range.ValidCount(), 2);
            range.Remove(10004, 1);
            Assert.AreEqual(range.ValidCount(), 1);
            range.Remove(10007, 1);
            Assert.AreEqual(range.ValidCount(), 0);
        }

        [TestMethod("RangeTest ExampleAckWithLossTest")]
        public void TestMethod30()
        {
            SmartRange range = new SmartRange();
            range.Add(10000);
            range.Add(10001);
            range.Add(10003);
            Assert.AreEqual(range.ValidCount(), 2);
            range.Add(10002);
            Assert.AreEqual(range.ValidCount(), 1);
            range.Remove(10000, 2);
            range.Remove(10003, 1);
            Assert.AreEqual(range.ValidCount(), 1);
            range.Remove(10002, 1);
            Assert.AreEqual(range.ValidCount(), 0);
            range.Add(10004);
            range.Add(10005);
            range.Add(10006);
            Assert.AreEqual(range.ValidCount(), 1);
            range.Remove(10004, 3);
            Assert.AreEqual(range.ValidCount(), 0);
            range.Add(10008);
            range.Add(10009);
            Assert.AreEqual(range.ValidCount(), 1);
            range.Remove(10008, 2);
            Assert.AreEqual(range.ValidCount(), 0);
        }

        [TestMethod("RangeTest AddLots")]
        public void TestMethod31()
        {
            SmartRange range = new SmartRange();
            for (int i = 0; i < 400; i += 2) {
                range.Add(i);
            }
            Assert.AreEqual(range.ValidCount(), 200);
            for (int i = 0; i < 398; i += 2) {
                range.Remove(i, 1);
            }
            Assert.AreEqual(range.ValidCount(), 1);
        }

        [TestMethod("RangeTest HitMax")]
        public void TestMethod32()
        {
            const int MaxCount = 16;
            SmartRange range = new SmartRange(MaxCount * QUIC_SUBRANGE.sizeof_Length);
            for (int i = 0; i < MaxCount; i++)
            {
                range.Add(i * 2);
            }
            Assert.AreEqual(range.ValidCount(), MaxCount);
            Assert.AreEqual(range.Min(), 0);
            Assert.AreEqual(range.Max(), (MaxCount - 1) * 2);
            range.Add(MaxCount * 2);
            Assert.AreEqual(range.ValidCount(), MaxCount);
            Assert.AreEqual(range.Min(), 2);
            Assert.AreEqual(range.Max(), MaxCount * 2);
            range.Remove(2, 1);
            Assert.AreEqual(range.ValidCount(), MaxCount - 1);
            Assert.AreEqual(range.Min(), 4);
            Assert.AreEqual(range.Max(), MaxCount * 2);
            range.Add(0);
            Assert.AreEqual(range.ValidCount(), MaxCount);
            Assert.AreEqual(range.Min(), 0);
            Assert.AreEqual(range.Max(), MaxCount * 2);
        }

        [TestMethod("RangeTest SearchZero")]
        public void TestMethod33()
        {
            SmartRange range = new SmartRange();
            var index = range.Find(25);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);
        }

        [TestMethod("RangeTest SearchOne")]
        public void TestMethod34()
        {
            SmartRange range = new SmartRange();
            range.Add(25);

            var index = range.Find(27);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.Find(26);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.Find(24);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);
            index = range.Find(23);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);

            index = range.Find(25);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
        }

        [TestMethod("RangeTest SearchTwo")]
        public void TestMethod35()
        {
            SmartRange range = new SmartRange();
            range.Add(25);
            range.Add(27);

            var index = range.Find(28);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 2);
            index = range.Find(26);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.Find(24);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);

            index = range.Find(27);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);
            index = range.Find(25);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
        }

        [TestMethod("RangeTest SearchThree")]
        public void TestMethod36()
        {
            SmartRange range = new SmartRange();
            range.Add(25);
            range.Add(27);
            range.Add(29);

            var index = range.Find(30);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 3);
            index = range.Find(28);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 2);
            index = range.Find(26);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.Find(24);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);

            index = range.Find(29);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 2);
            index = range.Find(27);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);
            index = range.Find(25);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
        }

        [TestMethod("RangeTest SearchFour")]
        public void TestMethod37()
        {
            SmartRange range = new SmartRange();
            range.Add(25);
            range.Add(27);
            range.Add(29);
            range.Add(31);

            var index = range.Find(32);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 4);
            index = range.Find(30);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 3);
            index = range.Find(28);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 2);
            index = range.Find(26);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.Find(24);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);

            index = range.Find(29);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 2);
            index = range.Find(27);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);
            index = range.Find(25);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
        }

        [TestMethod("RangeTest SearchRangeZero")]
        public void TestMethod38()
        {
            SmartRange range = new SmartRange();
            var index = range.FindRange(25, 17);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);
        }

        [TestMethod("RangeTest SearchRangeOne")]
        public void TestMethod39()
        {
            SmartRange range = new SmartRange();
            range.Add(25);

            var index = range.FindRange(27, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.FindRange(26, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.FindRange(22, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);
            index = range.FindRange(21, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);

            index = range.FindRange(23, 3);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
            index = range.FindRange(24, 3);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
            index = range.FindRange(25, 3);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
        }

        [TestMethod("RangeTest SearchRangeTwo")]
        public void TestMethod40()
        {
            SmartRange range = new SmartRange();
            range.Add(25);
            range.Add(30);

            var index = range.FindRange(32, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 2);
            index = range.FindRange(31, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 2);
            index = range.FindRange(26, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.FindRange(27, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.FindRange(28, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.FindRange(22, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);
            index = range.FindRange(23, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);

            index = range.FindRange(24, 2);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
            index = range.FindRange(24, 3);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
            index = range.FindRange(25, 2);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
            index = range.FindRange(29, 2);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);
            index = range.FindRange(29, 3);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);
            index = range.FindRange(30, 2);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);

            index = range.FindRange(24, 7);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
#if QUIC_RANGE_USE_BINARY_SEARCH
    Assert.AreEqual(index, 0);
#else
            Assert.AreEqual(index, 1);
#endif
            index = range.FindRange(25, 6);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
#if QUIC_RANGE_USE_BINARY_SEARCH
    Assert.AreEqual(index, 0);
#else
            Assert.AreEqual(index, 1);
#endif
        }

        [TestMethod("RangeTest SearchRangeThree")]
        public void TestMethod41()
        {
            SmartRange range = new SmartRange();
            range.Add(25);
            range.Add(30);
            range.Add(35);

            var index = range.FindRange(36, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 3);
            index = range.FindRange(32, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 2);
            index = range.FindRange(31, 3);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 2);
            index = range.FindRange(26, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.FindRange(27, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.FindRange(28, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 1);
            index = range.FindRange(22, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);
            index = range.FindRange(23, 2);
            Assert.IsTrue(MSQuicFunc.IS_INSERT_INDEX(index));
            Assert.AreEqual(MSQuicFunc.INSERT_INDEX_TO_FIND_INDEX(index), 0);

            index = range.FindRange(24, 2);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
            index = range.FindRange(24, 3);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
            index = range.FindRange(25, 2);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 0);
            index = range.FindRange(29, 2);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);
            index = range.FindRange(29, 3);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);
            index = range.FindRange(30, 2);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);

            index = range.FindRange(24, 7);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);
            index = range.FindRange(25, 6);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
            Assert.AreEqual(index, 1);

            index = range.FindRange(29, 7);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
#if QUIC_RANGE_USE_BINARY_SEARCH
    Assert.AreEqual(index, 1);
#else
            Assert.AreEqual(index, 2);
#endif
            index = range.FindRange(30, 6);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
#if QUIC_RANGE_USE_BINARY_SEARCH
    Assert.AreEqual(index, 1);
#else
            Assert.AreEqual(index, 2);
#endif

            index = range.FindRange(24, 12);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
#if QUIC_RANGE_USE_BINARY_SEARCH
    Assert.AreEqual(index, 1);
#else
            Assert.AreEqual(index, 2);
#endif
            index = range.FindRange(25, 11);
            Assert.IsTrue(MSQuicFunc.IS_FIND_INDEX(index));
#if QUIC_RANGE_USE_BINARY_SEARCH
    Assert.AreEqual(index, 1);
#else
            Assert.AreEqual(index, 2);
#endif
        }
    }
}
