using AKNet.Common;
using System.Diagnostics;

namespace OtherTest
{
    internal class InterlockedExTest
    {
        Stopwatch Stopwatch = Stopwatch.StartNew();
        public InterlockedExTest()
        {
           
        }

        ulong MMM = 0;
        public void Test()
        {
            this.Do();
            this.Do2();
        }

        private async Task Do()
        {
            var tasks = new Task[1000000];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    InterlockedEx.Increment(ref MMM);
                });
            }

            await Task.WhenAll(tasks);

            Console.WriteLine($"使用 Interlocked, 线程数量: {tasks.Length}, MMM: {MMM}");
        }

        long MMM2 = 0;
        private async Task Do2()
        {
            var tasks = new Task[1000000];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() => Interlocked.Increment(ref MMM2));
            }

            await Task.WhenAll(tasks);
            Console.WriteLine($"未使用 Interlocked 线程数量: {tasks.Length}, MMM: {MMM2}");
        }
    }
}
