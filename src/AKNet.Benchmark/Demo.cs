using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace AKNet.Benchmark
{
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    [SimpleJob(RuntimeMoniker.NativeAot80)]
    [SimpleJob(RuntimeMoniker.Net461)]
    [RPlotExporter] //一键出图
    public class Demo
    {
        [Benchmark(Baseline = true)]
        public string Concat3() => "a" + "b" + "c";

        [Benchmark]
        public string Interp() => $"a{"b"}c";
    }
}