using BenchmarkDotNet.Running;

namespace AKNet.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Md5VsSha256>();
            BenchmarkRunner.Run<Demo>();
        }
    }
}
